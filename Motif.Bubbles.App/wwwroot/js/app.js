// The Web App's own browser plumbing. Everything here is thin: the logic stays in C#.

export function fragment() { return location.hash; }
export function pageUrl() { return location.href; }

// Replaces the fragment without a history entry or a hashchange event.
export function setFragment(value) {
  const url = location.pathname + location.search + (value ? "#" + value : "");
  if (url !== location.pathname + location.search + location.hash) history.replaceState(null, "", url);
}

// Someone edited the address bar, or followed a repo link into this tab.
export function listenForFragment(dotnet) {
  window.addEventListener("hashchange", () => dotnet.invokeMethodAsync("OnFragment", location.hash));
}

export async function copy(text) {
  try { await navigator.clipboard.writeText(text); return true; } catch { return false; }
}

export function focus(selector) {
  const el = document.querySelector(selector);
  if (el) { el.focus(); el.select?.(); }
}

// Theme: follows the system, with an override kept in this browser only. Shared with the other Motif tools.
export function theme() {
  try { return localStorage.getItem("motif-theme") || "system"; } catch { return "system"; }
}

export function setTheme(value) {
  try { localStorage.setItem("motif-theme", value); } catch { /* storage may be blocked */ }
  if (value === "light" || value === "dark") document.documentElement.setAttribute("data-theme", value);
  else document.documentElement.removeAttribute("data-theme");
}

// Pan and zoom: the graph's .viewport group is moved and scaled here. Blazor never writes its transform,
// so redrawing the graph (a hover, a layer) keeps where it was panned to.
const views = new WeakMap();

function apply(graph) {
  const view = views.get(graph);
  graph.querySelector(".viewport")?.setAttribute("transform", `translate(${view.x} ${view.y}) scale(${view.k})`);
}

// A point on screen, in the SVG's own units.
function toSvg(svg, x, y) {
  const m = svg.getScreenCTM();
  return m ? new DOMPoint(x, y).matrixTransform(m.inverse()) : new DOMPoint(0, 0);
}

export function panAndZoom(selector) {
  const graph = document.querySelector(selector);
  if (!graph || views.has(graph)) return;
  const svg = graph.querySelector("svg");
  const view = { x: 0, y: 0, k: 1 };
  views.set(graph, view);

  svg.addEventListener("wheel", e => {
    e.preventDefault();
    const p = toSvg(svg, e.clientX, e.clientY);
    const k = Math.min(4, Math.max(0.25, view.k * Math.exp(-e.deltaY * 0.0015)));
    // Keep the point under the pointer where it is.
    view.x = p.x - (p.x - view.x) * k / view.k;
    view.y = p.y - (p.y - view.y) * k / view.k;
    view.k = k;
    apply(graph);
  }, { passive: false });

  let drag = null;
  svg.addEventListener("pointerdown", e => {
    if (e.button !== 0) return;
    drag = { p: toSvg(svg, e.clientX, e.clientY), x: view.x, y: view.y };
    svg.setPointerCapture(e.pointerId);
  });
  svg.addEventListener("pointermove", e => {
    if (!drag) return;
    const p = toSvg(svg, e.clientX, e.clientY);
    view.x = drag.x + p.x - drag.p.x;
    view.y = drag.y + p.y - drag.p.y;
    graph.classList.add("panning");
    apply(graph);
  });
  const end = () => { drag = null; graph.classList.remove("panning"); };
  svg.addEventListener("pointerup", end);
  svg.addEventListener("pointercancel", end);

  // The tooltip follows the pointer, and flips to stay inside the graph.
  const tip = graph.querySelector(".tip");
  graph.addEventListener("mousemove", e => {
    if (!tip) return;
    const box = graph.getBoundingClientRect();
    let x = e.clientX - box.left + 16, y = e.clientY - box.top + 16;
    if (x + tip.offsetWidth > box.width) x -= tip.offsetWidth + 32;
    if (y + tip.offsetHeight > box.height) y -= tip.offsetHeight + 32;
    tip.style.left = Math.max(4, x) + "px";
    tip.style.top = Math.max(4, y) + "px";
  });
}

export function fit(selector) {
  const graph = document.querySelector(selector);
  const view = graph && views.get(graph);
  if (!view) return;
  Object.assign(view, { x: 0, y: 0, k: 1 });
  apply(graph);
}
