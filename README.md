# ISO to BIN/CUE Converter

## Overview

This repository contains a .NET Core console application designed to convert CD image files from ISO/CUE format to BIN/CUE format. 
This conversion can be useful for compatibility with software or emulators that require BIN/CUE files.

## Features

- Convert single ISO/CUE file pair to BIN/CUE.
- Batch convert multiple ISO/CUE file pairs located in first-level subdirectories.
- Preserves track information including audio and data tracks.
- Handles PREGAP information if present in the original CUE sheet.

## Requirements

- .NET 8.0 or higher

## Installation

1. Clone the repository:
    \```bash
    git clone https://github.com/andrewboudreau/ConvertIsoToBin.git
    \```
2. Navigate to the project directory:
    \```bash
    cd ConvertIsoToBin
    \```

## Usage

### Convert a Single ISO/CUE File Pair

Run the following command:

\```bash
dotnet run <path/to/iso/file>
\```

### Convert Multiple ISO/CUE File Pairs

To batch convert ISO/CUE file pairs located in first-level subdirectories of a given directory, run:

\```bash
dotnet run <path/to/directory>
\```

## Output

The program will generate BIN and CUE files in the same directory as the original ISO file, with the new files having a prefix of `_`.

## Limitations

- Assumes that the ISO and CUE files are correctly formatted and intact.
- Assumes that each track is either AUDIO or MODE1/2352. Other track types are not supported.
- Assumes the entire ISO file can fit into memory for processing.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.
