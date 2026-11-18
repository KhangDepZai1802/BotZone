"use client";
import { useMemo, useSyncExternalStore } from "react";
import { useSearchParams } from "next/navigation";

const eventName = "botzone-querychange";
const readQuery = () => window.location.search.slice(1);
const subscribe = (notify: () => void) => {
  window.addEventListener("popstate", notify);
  window.addEventListener(eventName, notify);
  return () => {
    window.removeEventListener("popstate", notify);
    window.removeEventListener(eventName, notify);
  };
};

// Next integrates native history with its router. The synchronous subscription
// also keeps controlled inputs and locale links current during its transition.
export function useQueryParams() {
  const routeQuery = useSearchParams();
  const serialized = useSyncExternalStore(subscribe, readQuery, () => routeQuery.toString());
  return useMemo(() => new URLSearchParams(serialized), [serialized]);
}

export function updateQueryUrl(href: string, replace = false) {
  window.history[replace ? "replaceState" : "pushState"](null, "", href);
  window.dispatchEvent(new Event(eventName));
}
