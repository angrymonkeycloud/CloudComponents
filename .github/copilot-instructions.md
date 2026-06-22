# Copilot Instructions

## Project Guidelines
- For this repository, update the .less source files instead of generated .css files; prefer nested class structure like .cloudgrid { &-headcell { ... } }, use ::deep for icons/styles targeting content outside the current component under Blazor CSS isolation, keep the main class hard-coded in markup with additional runtime classes appended via methods, and expose developer-friendly class composition in CloudGrid markup.