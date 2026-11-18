import React, { useState } from "react";
import { createRoot } from "react-dom/client";
import { Button, Field, Modal, Select } from "../../src/components/ui";

// Isolated browser fixture for the actual shared components. No API or sessions.
function Fixture() {
  const [open, setOpen] = useState(false);
  const [product, setProduct] = useState("");
  const [configuration, setConfiguration] = useState("");
  const [result, setResult] = useState("");
  return <main className="page-wrap">
    <Button onClick={() => setOpen(true)}>Open selection form</Button>
    <Modal open={open} onOpenChange={setOpen} title="Component selection form" description="Local component fixture with no backend requests.">
      <form className="form-stack" onSubmit={(event) => {
        event.preventDefault();
        setResult(JSON.stringify(Object.fromEntries(new FormData(event.currentTarget))));
      }}>
        <Field label="Product">
          <Select name="product" required value={product} onChange={(event) => { setProduct(event.target.value); setConfiguration(""); }}>
            <option value="">Choose a product</option>
            <option value="alpha">Alpha</option>
            <option value="disabled" disabled>Unavailable option</option>
            <option value="beta">Beta</option>
            {Array.from({ length: 32 }, (_, i) => <option key={i} value={`option-${i}`}>Long product option {i + 1}</option>)}
          </Select>
        </Field>
        <Field label="Configuration" hint="Optional; an empty value must remain empty in FormData.">
          <Select name="configuration" value={configuration} disabled={!product} onChange={(event) => setConfiguration(event.target.value)}>
            <option value="">Not specified</option>
            {product && <option value={`${product}-large`}>Large configuration</option>}
          </Select>
        </Field>
        <div className="actions"><Button type="submit">Submit locally</Button><Button variant="outline" type="reset">Reset native form</Button></div>
        <output aria-label="Submitted values">{result}</output>
      </form>
    </Modal>
  </main>;
}

createRoot(document.getElementById("fixture")!).render(<Fixture />);
