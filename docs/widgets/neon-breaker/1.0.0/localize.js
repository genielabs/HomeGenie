// Translate author-supplied labels. Module names, readings and Material icon ligatures
// are not part of the catalog. Widget controls stay usable while the catalog loads.
export async function localize(widget, statusFields = []) {
  if (widget.labelsDisposed || !widget.translate) return;
  const defaults = widget.labels;
  const entries = await Promise.all(Object.entries(defaults).map(([key, fallback]) =>
    new Promise(resolve => {
      widget.translate(key, {scope: 'widget', fallback}).subscribe(value => {
        resolve([key, typeof value === 'string' ? value : fallback]);
      });
    })
  ));
  if (widget.labelsDisposed) return;
  const labels = Object.fromEntries(entries);
  widget.labels = labels;
  const root = widget.view().get();
  const selector = '[data-label-text],[data-label-aria]';
  const elements = [...root.querySelectorAll(selector)];
  if (root.matches?.(selector)) elements.unshift(root);
  for (const element of elements) {
    const aria = element.getAttribute('data-label-aria');
    if (aria && element.getAttribute('aria-label') === defaults[aria]) {
      element.setAttribute('aria-label', labels[aria]);
    }
    for (const key of (element.getAttribute('data-label-text') || '').split(' ')) {
      if (!key) continue;
      for (const node of element.childNodes) {
        if (node.nodeType === Node.TEXT_NODE && node.textContent.trim() === defaults[key]) {
          node.textContent = node.textContent.replace(defaults[key], () => labels[key]);
          break;
        }
      }
    }
  }
  // An initial status may already have been replaced by a response or user action.
  const model = widget.model();
  for (const field of statusFields) {
    const key = Object.keys(defaults).find(k => model[field] === defaults[k]);
    if (key) model[field] = labels[key];
  }
  widget.onLabelsReady?.();
}
