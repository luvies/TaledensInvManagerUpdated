# Taleden's Inventory Manager - Updated (Unofficial)

This is the source code for the updated version of TIM. All changes and updates will go here first, and once I believe that the script is stable enough, then I will push it to the workshop. I may also make a beta workshop item so people can help test it, however this isn't a priority.

The main reason for the whole solution, is that it allow much better intellisense when editing, which thanks to Keen's lack of documentation, is one of the only things I can use really. The script source will not work in the Programmable Block directly, you must build the solution using VS to get the actual code.

The workshop item can be found [here](http://steamcommunity.com/sharedfiles/filedetails/?id=1268188438).

## Intellisense

To make the repo better for anyone to grab and use, I've set up the project to use the [MDK-SE](https://github.com/malware-dev/MDK-SE). Install the [latest release](https://github.com/malware-dev/MDK-SE/releases) to your visual studio 2019 instance to get the tools set uup.

Once installed the first time you open the project it will ask you to repair the project, it should automatically pick up your install location of your Space Engineers game and use it during the development process. You may need to close and re-open the project after doing this to get Intellisense to work. If it did not automatically pick up the settings or you would like it to use a different installation of Space Engineers you can right click on the project and choose "MDK Script Options" to change the settings.

## Building

To generate a PB-compatible file, right click on the project and choose "MDK Deploy Script" and it will automatically deploy the script to a location that can be picked up by the game automatically.

## Todo

These are all the things that we are planning on doing or have done to the code. This will be added as requests come in or when we think something needs updating.

- [x] Minify main code block
- [x] Shuffle code to make file more readable
- [ ] Add comments to classes and functions to make it clearer
- [x] Deprecate the cycle argument for dynamic run-cycles
- [ ] Update LCD system
    - [ ] Simpify code to reduce execution time
    - [ ] Add Automatic LCDs support for better display
- [ ] Allow tag reading from Custom Data