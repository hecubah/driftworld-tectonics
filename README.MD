# Driftworld Tectonics
**Warning: this release is the first published, errors in running the project are quite possible! Please comment if you would like to try the project out and are unable to.**
## Introduction
Driftworld Tectonics is a project written for Unity editor, used to create basic forms of planets. It utilizes ideas and methods described in an article by Yann Cortial et al. in 2019 (see the main documentation file). Planets are created by reading basic template topology, performing a simplified tectonic simulation of the planet’s crust and then exporting the raw data to customized binary files. Users can display various overlays of the planet during the simulation, as well as see the surface elevation mesh. The project requires running within Unity framework. The scene is not meant to be running, only GUI tools are available in the Unity editor.
## Acknowledgements
I would like to thank Dr. Yann Cortial of the National Institute of Applied Sciences of Lyon for discussing his article. His answers to my various inquiries helped me decide the scope and form of Driftworld. I would also like to thank Dr. Daniel Meister of AMD Japan Co. Ltd. for his comments on the use of bounding volume hiearchy algorithms. Driftworld implements a part of an algorithm described in one of his publications. More thanks to PROWAREtech for allowing me to include an adapted version of an example implementation of Mersenne Twister random number generator in
the project.

I would like to thank Ben Golus for his help with UV texture mapping and his advice on texturing.

Special thanks to my friends Vilém, Matyáš and Michal and other members of our Discord server for discussing ideas and for their feedback and support.

The project was created using Unity Editor, lately in its version 2021.3.1f1. The C# code is kept in a MS Visual Studio Community 2022 project, image materials come from Unity Editor screenshots, Geogebra projects, Blender and Dia. Documentation uses LaTeX in its TeX Live implementation.
## Repository contents
This repository includes all source codes and necessary files to run Driftworld Tectonics, provided the user has Unity installed. MS Visual Studio is recommended for its compatibility with Unity (also, the project was mostly written in it).
## Documentation
The Doc directory contains a PDF file with the documentation overview ([link here](/Docs/driftworld-tectonics.pdf)). It is written in a thesis-like style, but it is not intended as such. There is also a lot of dreamy rambling, I implore any would-be reader to take it easy on me (thanks!).

Shield: [![CC BY 4.0][cc-by-shield]][cc-by]

This work is licensed under a
[Creative Commons Attribution 4.0 International License][cc-by].

[![CC BY 4.0][cc-by-image]][cc-by]

[cc-by]: http://creativecommons.org/licenses/by/4.0/
[cc-by-image]: https://i.creativecommons.org/l/by/4.0/88x31.png
[cc-by-shield]: https://img.shields.io/badge/License-CC%20BY%204.0-lightgrey.svg
