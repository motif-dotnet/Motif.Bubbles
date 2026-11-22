// The Local Folders Fitting: the folder picker, and folders dropped onto the
// page. Each comes back as a listing, { number, name, paths }, whose files
// are read one at a time. The rules are in the C# adapter.

// Not worth walking into: they can hold many thousands of files.
const skipped = new Set(["bin", "obj", "node_modules", ".git", ".vs"]);
const kept = 4;
const listings = new Map();
let count = 0;
let latest = Promise.resolve({ number: 0, name: "", paths: [] });

// Keeps a listing's files, by path, for read, and lists them.
function keep(name, files) {
  const number = ++count;
  listings.set(number, files);
  listings.delete(number - kept);
  return { number, name, paths: [...files.keys()] };
}

function wanted(path) {
  return !path.split("/").slice(0, -1).some(part => skipped.has(part));
}

async function entries(dir) {
  const reader = dir.createReader();
  const all = [];
  for (;;) {
    const batch = await new Promise((resolve, reject) => reader.readEntries(resolve, reject));
    if (batch.length === 0) return all;
    all.push(...batch);
  }
}

async function walk(entry, path, files) {
  if (entry.isFile) {
    files.set(path, await new Promise((resolve, reject) => entry.file(resolve, reject)));
  } else if (!skipped.has(entry.name)) {
    for (const child of await entries(entry)) await walk(child, path ? `${path}/${child.name}` : child.name, files);
  }
}

// A folder dropped is walked from its root; loose files are listed by name.
async function dropped(items) {
  const files = new Map();
  const folder = items.find(e => e.isDirectory);
  if (folder) {
    for (const child of await entries(folder)) await walk(child, child.name, files);
    return keep(folder.name, files);
  }
  for (const entry of items) await walk(entry, entry.name, files);
  return keep("", files);
}

// Catches drops in the capture phase, so the walk has started when the page hears about the drop.
// The browser only lets a drop's entries be taken while its event runs, so that stays here.
export function listen() {
  const hasFiles = e => Array.from(e.dataTransfer?.types ?? []).includes("Files");
  window.addEventListener("dragover", e => { if (hasFiles(e)) e.preventDefault(); }, true);
  window.addEventListener("drop", e => {
    if (!hasFiles(e)) return;
    e.preventDefault();
    const items = Array.from(e.dataTransfer.items).map(i => i.webkitGetAsEntry?.()).filter(Boolean);
    latest = dropped(items);
  }, true);
}

export function latestDrop() {
  return latest;
}

export function pick() {
  return new Promise((resolve, reject) => {
    const input = document.createElement("input");
    input.type = "file";
    input.webkitdirectory = true;
    input.addEventListener("change", () => {
      const all = Array.from(input.files ?? []);
      const name = all[0]?.webkitRelativePath.split("/")[0] ?? "";
      const files = new Map();
      for (const file of all) {
        const path = file.webkitRelativePath.split("/").slice(1).join("/");
        if (wanted(path)) files.set(path, file);
      }
      resolve(keep(name, files));
    });
    input.addEventListener("cancel", () => resolve({ number: 0, name: "", paths: [] }));
    input.addEventListener("error", reject);
    input.click();
  });
}

export async function read(number, path) {
  const file = listings.get(number)?.get(path);
  return file ? await file.text() : "";
}
