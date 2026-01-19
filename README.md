# PLT — Programming Language Translator

PLT is an experimental **source-to-source programming language translator**.

It converts code from one language into a **language-neutral intermediate representation (IR)**, then emits equivalent code in another language.

Current focus:
- JavaScript → IR → Python
- JavaScript → IR → C

This project is intentionally built like a small compiler:
- frontend (parse / lower source language)
- IR (language-independent meaning)
- backend (emit target language)

---

## Why an IR?

Directly translating language A → language B does not scale.

PLT instead uses a central **Intermediate Representation (IR)** that captures *semantics*, not syntax.  
Each language only needs:
- one frontend (→ IR)
- one backend (IR → target)

This avoids N×M translators and keeps the system extensible.

---

## Current Features

- ✅ Language-neutral IR with pretty printer
- ✅ JavaScript mini-frontend (MVP)
  - Supports:
    ```js
    // optional comment
    console.log("Hello, world!")
    ```
- ✅ Python backend
- ✅ C backend (with `main()` and `printf`)
- ✅ CLI tool with flags
- ✅ `--print-ir` for debugging

---

## Example

### Input (`hello.js`)
```js
// Prints "Hello, world!" to the console
console.log("Hello, world!")
```

### Command

```powershell
dotnet run --project .\PLT.CLI\ -- --from js --to c examples\hello.js --print-ir -o examples\out\hello.c
```

### IR

```
IrProgram
  // Prints "Hello, world!" to the console
  ExprStmt
    Intrinsic "print"
      Literal "Hello, world!"
```

### Output (`hello.c`)

```c
#include <stdio.h>

int main(void) {
    // Prints "Hello, world!" to the console
    printf("%s\n", "Hello, world!");
    return 0;
}
```

---

## CLI Usage

```text
plt --from js --to python <input.js> [-o output.py] [--print-ir]
plt --from js --to c      <input.js> [-o output.c]  [--print-ir]
```

Examples:

```powershell
dotnet run --project .\PLT.CLI\ -- --from js --to python examples\hello.js
dotnet run --project .\PLT.CLI\ -- --from js --to c examples\hello.js -o hello.c
```

---

## Project Structure

```
PLT/
├─ PLT.CORE/
│  ├─ IR/            # IR node definitions + pretty printer
│  ├─ Frontends/     # Source language → IR
│  └─ Backends/      # IR → target language
│
├─ PLT.CLI/          # Command-line interface
├─ PLT.TESTS/        # Tests
└─ examples/
```

---

## Goals / Roadmap

Short term:

* Expand JS frontend (multiple args, numbers, booleans)
* Smarter default output filenames
* More tests

Longer term:

* Real JS parser (tree-sitter or Babel AST)
* More IR nodes (variables, expressions, control flow)
* More target languages
* Optional runtime support library

---

## Philosophy

This project prioritizes:

* correctness over cleverness
* explicit semantics over syntax tricks
* debuggability (IR inspection is first-class)

It is not meant to be a perfect translator between all languages.
It *is* meant to explore how translation works when treated as a compiler problem.

---

## Status
🚧 Actively developed
⚠️ Not production-ready
🧠 Built for learning, experimentation, and fun

---

# This is an experiment being create by the GitHub workspaces AI to see how capable and effective it is.
