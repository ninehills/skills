import { useState } from 'react';

export default function App() {
  const [open, setOpen] = useState(false);

  return (
    <main className="page">
      <button data-testid="open-modal" type="button" onClick={() => setOpen(true)}>
        Open Modal
      </button>
      {open && (
        <div className="modal-backdrop">
          <div className="modal" role="dialog" aria-modal="true">
            <h1 className="hero-title">Modal Heading</h1>
            <p className="hero-hook">Conditionally rendered — only mounts on click.</p>
            <button data-testid="close-modal" type="button" onClick={() => setOpen(false)}>
              Close
            </button>
          </div>
        </div>
      )}
    </main>
  );
}
