"use client";

import * as React from "react";
import * as SelectPrimitive from "@radix-ui/react-select";
import { Check, ChevronDown, ChevronUp } from "lucide-react";
import { useOptionalStore } from "./client";

type NativeProps = React.SelectHTMLAttributes<HTMLSelectElement>;
type Option = { value: string; label: string; disabled?: boolean; group?: string };

function textOf(node: React.ReactNode): string {
  return React.Children.toArray(node).map((child) =>
    React.isValidElement<{ children?: React.ReactNode }>(child)
      ? textOf(child.props.children) : String(child),
  ).join("");
}

function readOptions(children: React.ReactNode, group?: string): Option[] {
  return React.Children.toArray(children).flatMap((child): Option[] => {
    if (!React.isValidElement<React.OptionHTMLAttributes<HTMLOptionElement> & { label?: string }>(child)) return [];
    if (child.type === "option") return [{
      value: String(child.props.value ?? textOf(child.props.children)),
      label: child.props.label ?? textOf(child.props.children),
      disabled: child.props.disabled, group,
    }];
    return readOptions(child.props.children, child.type === "optgroup" ? child.props.label : group);
  });
}

/** Keep native option/value/onChange and FormData semantics behind a Radix UI. */
export function Select({ children, value, defaultValue, onChange, className = "", disabled, required, name, id, onInvalid, ...props }: NativeProps) {
  const store = useOptionalStore();
  const errorId = React.useId();
  const options = readOptions(children);
  const [internal, setInternal] = React.useState(String(defaultValue ?? options[0]?.value ?? ""));
  const [invalid, setInvalid] = React.useState(false);
  const native = React.useRef<HTMLSelectElement>(null);
  const trigger = React.useRef<HTMLButtonElement>(null);
  const selected = value === undefined ? internal : String(value);
  const selectedOption = options.find((option) => option.value === selected);
  // Prefix every value: Radix reserves the empty string, while native required
  // validation and optional "not specified" options must keep their real value.
  const encode = (v: string) => `option:${v}`;

  React.useEffect(() => {
    const form = native.current?.form;
    const reset = () => { setInternal(String(defaultValue ?? options[0]?.value ?? "")); setInvalid(false); };
    form?.addEventListener("reset", reset);
    return () => form?.removeEventListener("reset", reset);
  }, [defaultValue, options[0]?.value]);

  return <>
    <SelectPrimitive.Root
      value={encode(selected)} disabled={disabled}
      onValueChange={(encoded) => {
        const element = native.current;
        if (!element) return;
        const next = encoded.slice("option:".length);
        Object.getOwnPropertyDescriptor(HTMLSelectElement.prototype, "value")?.set?.call(element, next);
        element.dispatchEvent(new Event("change", { bubbles: true }));
      }}
    >
      <SelectPrimitive.Trigger
        ref={trigger} id={id} className={`input select-trigger ${className}`}
        aria-label={props["aria-label"]} aria-labelledby={props["aria-labelledby"]}
        aria-describedby={[props["aria-describedby"], invalid ? errorId : undefined].filter(Boolean).join(" ") || undefined} aria-required={required}
        aria-invalid={props["aria-invalid"] ?? (invalid || undefined)}
        title={props.title} style={props.style} autoFocus={props.autoFocus} tabIndex={props.tabIndex}
      >
        <SelectPrimitive.Value>{selectedOption?.label ?? "—"}</SelectPrimitive.Value>
        <SelectPrimitive.Icon className="select-chevron"><ChevronDown size={16} aria-hidden /></SelectPrimitive.Icon>
      </SelectPrimitive.Trigger>
      <SelectPrimitive.Portal>
        <SelectPrimitive.Content className="select-content" position="popper" sideOffset={7} collisionPadding={12} align="start">
          <SelectPrimitive.ScrollUpButton className="select-scroll"><ChevronUp size={16} aria-hidden /></SelectPrimitive.ScrollUpButton>
          <SelectPrimitive.Viewport className="select-viewport">
            {options.map((option, index) => <React.Fragment key={option.value}>
              {option.group && option.group !== options[index - 1]?.group && <div className="select-group-label">{option.group}</div>}
              <SelectPrimitive.Item className="select-item" value={encode(option.value)} disabled={option.disabled} textValue={option.label}>
                <SelectPrimitive.ItemText>{option.label}</SelectPrimitive.ItemText>
                <SelectPrimitive.ItemIndicator className="select-check"><Check size={16} aria-hidden /></SelectPrimitive.ItemIndicator>
              </SelectPrimitive.Item>
            </React.Fragment>)}
          </SelectPrimitive.Viewport>
          <SelectPrimitive.ScrollDownButton className="select-scroll"><ChevronDown size={16} aria-hidden /></SelectPrimitive.ScrollDownButton>
        </SelectPrimitive.Content>
      </SelectPrimitive.Portal>
    </SelectPrimitive.Root>
    <select
      {...props} ref={native} className="select-native" aria-hidden="true" tabIndex={-1}
      aria-label={undefined} aria-labelledby={undefined} aria-describedby={undefined}
      value={selected} name={name} disabled={disabled} required={required}
      onChange={(event) => { setInternal(event.target.value); setInvalid(false); onChange?.(event); }}
      onInvalid={(event) => { event.preventDefault(); setInvalid(true); trigger.current?.focus(); onInvalid?.(event); }}
    >{children}</select>
    {invalid && <span className="select-error" id={errorId} role="alert">{store?.tr("Vui lòng chọn một lựa chọn.", "Please choose an option.") ?? "Please choose an option."}</span>}
  </>;
}
