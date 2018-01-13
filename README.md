# Taleden's Inventory Manager - Updated (Unofficial)

This is the source code for the updated version of TIM. All changes and updates will go here first, and once I believe that the script is stable enough, then I will push it to the workshop. I may also make a beta workshop item so people can help test it, however this isn't a priority.

The main reason for the whole solution, is that it allow much better intellisense when editing, which thanks to Keen's lack of documentation, is one of the only things I can use really. The script source will not work in the Programmable Block directly, you must build the solution using VS to get the actual code.

The workshop item can be found [here](http://steamcommunity.com/sharedfiles/filedetails/?id=1268188438).

## Building

To generate a PB-compatible file, build the project and use the source generated in `bin/out/Script.cs`. To build, use Visual Studio, as I have set up the project to run the necessary commands that are needed to build the script. This can be done via Build->Build Solution via the top dropdowns, or Ctrl-Shift-B.

## Todo

These are all the things that we are planning on doing or have done to the code. This will be added as requests come in or when we think something needs updating.

- [x] Minify main code block
- [ ] Shuffle code to make file more readable
- [ ] Add comments to classes and functions to make it clearer
- [ ] Deprecate the cycle argument for dynamic run-cycles
- [ ] Update LCD system
    - [ ] Simpify code to reduce execution time
    - [ ] Add Automatic LCDs support for better display