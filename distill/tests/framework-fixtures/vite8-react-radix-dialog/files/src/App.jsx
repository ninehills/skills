import { useState } from 'react';
import * as DialogPrimitive from '@radix-ui/react-dialog';

export default function App() {
  const [open, setOpen] = useState(false);

  return (
    <main className="page">
      <DialogPrimitive.Root open={open} onOpenChange={setOpen}>
        <DialogPrimitive.Trigger asChild>
          <button data-testid="open-dialog" type="button">Open Dialog</button>
        </DialogPrimitive.Trigger>
        <DialogPrimitive.Portal>
          <DialogPrimitive.Overlay className="dialog-overlay" />
          <DialogPrimitive.Content className="dialog-content">
            <aside className="lightbox">
              <DialogPrimitive.Title asChild>
                <h1 className="hero-title">Modal Heading</h1>
              </DialogPrimitive.Title>
              <p className="hero-hook">Inside a Radix DialogPrimitive.Portal.</p>
              <button data-testid="close-dialog" type="button" onClick={() => setOpen(false)}>
                Close
              </button>
            </aside>
          </DialogPrimitive.Content>
        </DialogPrimitive.Portal>
      </DialogPrimitive.Root>
    </main>
  );
}
