# Glossary

The words this project uses, and what they mean. People and agents use these terms in code, docs, issues and discussion, and avoid the words listed under **Avoid**. A new concept gets an entry, marked *proposed*, in the same change that introduces it. An entry with no status is agreed.

The first entries came from the sketch ([sketch.motifsketch.json](sketch.motifsketch.json)): **Sketch** gives the id of the element or port each one comes from. The format is described in the [Motif Glossary design note](https://github.com/motif-dotnet/.github/blob/main/design/glossary.md).

## Bubble

One architectural object of the repo, drawn as a circle: the Domain, the User Tasks or User Queries, the Application, a driven port, a real adapter, a fake, the specs or the browser specs. Its area is its lines of code, its outline its kind (solid for real code, dashed for a fake, plain for a port, the accent colour for specs), and its ring its mutants. A name that fits goes inside, with its count of specs or members, such as ×79.

- **Kind:** domain concept
- **Avoid:** node, project (a project can hold several bubbles, as an adapters project does)
- **In code:** `Bubble`, `BubbleKind`
- **Status:** proposed
- **See also:** [Column](#column), [Mutation ring](#mutation-ring)

## Code Folders

A Motif repo's folder on the Reviewer's computer, opened or dropped in, with the Stryker reports in its StrykerOutput/ folder.

- **Kind:** storage
- **Status:** proposed
- **Sketch:** `b0b1a005`

## Column

One of the graph's fixed places, left to right, following ports and adapters: browser checks, driving, User port, Application, driven ports, real adapters, fakes and shared fakes. Every bubble goes in the column its kind of object belongs in, so the same repo always looks the same.

- **Kind:** domain concept
- **In code:** `Column`, `Columns.Heading`
- **Status:** proposed
- **See also:** [Lane](#lane)

## For opening folders

Reads the code in a folder the Reviewer opens or drops in, and the latest Stryker report in its StrykerOutput/ folder.

- **Kind:** port
- **Status:** proposed
- **Sketch:** `b0b1c003`

## For opening repos

Reads a public repo's code from its default branch, and its latest Stryker report from its Pages site if it has one.

- **Kind:** port
- **Status:** proposed
- **Sketch:** `b0b1c002`

## GitHub

Public repos' code, read from the default branch.

- **Kind:** external system
- **Status:** proposed
- **Sketch:** `b0b1a003`

## GitHub Pages

Where a repo's CI publishes its latest Stryker report, motif/mutation-report.json.

- **Kind:** external system
- **Status:** proposed
- **Sketch:** `b0b1a004`

## Lane

A place down the graph, counted from the top. Each driven port has a row of its own, as many lanes tall as it has real adapters or fakes, ports sorted by name. The bubbles on the left are spread evenly over all the lanes.

- **Kind:** domain concept
- **Avoid:** slot, track
- **In code:** `Bubble.Lane`, `Graph.Lanes`
- **Status:** proposed
- **See also:** [Column](#column)

## Layer

One part of what the graph draws, which the Reviewer can turn off to see what it adds: size by lines of code, mutation rings, each kind of link, and column bands. All are on at the start but wiring links, which cross the rest.

- **Kind:** domain concept
- **Avoid:** filter, toggle
- **In code:** `Layer`, `LayerChoice`
- **Status:** proposed

## Link

A line from one bubble to another, saying how it depends on it: it **checks** through it (specs and browser specs), **uses** it, **implements** it (an adapter or fake, its port), **extends** it (a fake, a shared fake) or **wires** it up (the composition root, or the specs standing in for it).

- **Kind:** domain concept
- **Avoid:** edge, arrow, dependency
- **In code:** `Link`, `LinkKind`
- **Status:** proposed
- **See also:** [Layer](#layer)

## Mutation ring

The ring around a bubble, showing what Stryker found in its code: mutants **caught** by a spec, mutants that **survived** every spec, and mutants **no spec reaches**. Code Stryker didn't mutate has no ring, and when the graph has rings, real code with none is hatched.

- **Kind:** domain concept
- **Avoid:** coverage ring, score ring
- **In code:** `Mutation`
- **Status:** proposed
- **See also:** [Mutation score](#mutation-score), [Stale ring](#stale-ring), [Stryker report](#stryker-report)

## Mutation score

The share of a bubble's mutants that its specs caught, as a whole percentage. Shown when hovering a bubble, not inside it, as the ring already shows it.

- **Kind:** domain concept
- **Avoid:** coverage
- **In code:** `Mutation.Score`
- **Status:** proposed
- **See also:** [Mutation ring](#mutation-ring)

## Reviewer

Opens or drops a Motif repo's folder, or names a public repo, and looks over its architecture and how well its specs check it.

- **Kind:** actor
- **Status:** proposed
- **Sketch:** `b0b1a002`

## Shared fake

Code fakes share, in a folder of the Fakes project named for no port, which a port's fake builds on (as Motif Sketch's FakeFiles). It sits in the last column, halfway down between the fakes that extend it.

- **Kind:** domain concept
- **Avoid:** other fake (a fake in a folder named for no port that no port's fake builds on)
- **In code:** `Column.SharedFakes`, `LinkKind.Extends`
- **Status:** proposed
- **See also:** [Link](#link)

## Stale ring

A mutation ring older than the code: a file in its bubble has changed since the Stryker report was made, or isn't in the report at all. It's drawn faded, and a note says how many bubbles have one.

- **Kind:** domain concept
- **Avoid:** old ring, outdated ring
- **In code:** `Mutation.Stale`
- **Status:** proposed
- **See also:** [Mutation ring](#mutation-ring)

## Stryker report

Stryker's JSON report of a run, which the mutation rings come from: from a folder, the latest in its StrykerOutput/ folder; from a repo, the copy its CI publishes to its Pages site, motif/mutation-report.json. It's never committed.

- **Kind:** domain concept
- **Avoid:** coverage report
- **In code:** `MutationReport`, `FoundReport`, `StrykerRun`
- **Status:** proposed
- **See also:** [GitHub Pages](#github-pages), [Code Folders](#code-folders)

## User

- **Kind:** port
- **Status:** proposed
- **Sketch:** `b0b1c001`
