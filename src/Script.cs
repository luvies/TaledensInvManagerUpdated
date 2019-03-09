/*-*/
// Base file from
// https://github.com/Gorea235/SpaceEngineers_IngameScriptingBase
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRageMath;
using VRage.Game.ModAPI.Ingame;
using VRage.Game;

namespace Scripts.TIM
{
    class Program : MyGridProgram
    {
        /*-*/
        /*
Taleden's Inventory Manager - Updated (Unofficial)
version 1.7.7 (2019-03-07)

Unoffical maintained version of TIM.

Steam Workshop: http://steamcommunity.com/sharedfiles/filedetails/?id=1268188438
User's Guide:   http://steamcommunity.com/sharedfiles/filedetails/?id=546909551

Source code:
    Since this script is minimised to reduce size and get round PB limits, you won't be able
    to edit this script directly. To view the source code, and possibly give contributions,
    please head to https://github.com/Gorea235/TaledensInvManagerUpdated

*******************
BASIC CONFIGURATION

These are the main settings for TIM. They allow you to adjust how often the script will
update, and the maximum load of each call before deferring execution to the next call.
*/
        // whether to use real time (second between calls) or pure UpdateFrequency
        // for update frequency
        readonly bool USE_REAL_TIME = false;

        // how often the script should update
        //     UpdateFrequency.None      - No automatic updating (manual only)
        //     UpdateFrequency.Once      - next tick (is unset after run)
        //     UpdateFrequency.Update1   - update every tick
        //     UpdateFrequency.Update10  - update every 10 ticks
        //     UpdateFrequency.Update100 - update every 100 ticks
        // (if USE_REAL_TIME == true, this is ignored)
        const UpdateFrequency UPDATE_FREQUENCY = UpdateFrequency.Update100;

        // How often the script should update in milliseconds
        // (if USE_REAL_TIME == false, this is ignored)
        const int UPDATE_REAL_TIME = 1000;

        // The maximum run time of the script per call.
        // Measured in milliseconds.
        const double MAX_RUN_TIME = 35;

        // The maximum percent load that this script will allow
        // regardless of how long it has been executing.
        const double MAX_LOAD = 0.8;

/*
***********************
ADVANCED CONFIGURATION

The settings below may be changed if you like, but read the notes and remember
that any changes will be reverted when you update the script from the workshop.
*/

        // Each "Type/" section can have multiple "/Subtype"s, which are formatted like
        // "/Subtype,MinQta,PctQta,Label,Blueprint". Label and Blueprint specified only
        // if different from Subtype, but Ingot and Ore have no Blueprint. Quota values
        // are based on material requirements for various blueprints (some built in to
        // the game, some from the community workshop).
        const string DEFAULT_ITEMS = @"
AmmoMagazine/
/Missile200mm
/NATO_25x184mm,,,,NATO_25x184mmMagazine
/NATO_5p56x45mm,,,,NATO_5p56x45mmMagazine

Component/
/BulletproofGlass,50,2%
/Computer,30,5%,,ComputerComponent
/Construction,150,20%,,ConstructionComponent
/Detector,10,0.1%,,DetectorComponent
/Display,10,0.5%
/Explosives,5,0.1%,,ExplosivesComponent
/Girder,10,0.5%,,GirderComponent
/GravityGenerator,1,0.1%,GravityGen,GravityGeneratorComponent
/InteriorPlate,100,10%
/LargeTube,10,2%
/Medical,15,0.1%,,MedicalComponent
/MetalGrid,20,2%
/Motor,20,4%,,MotorComponent
/PowerCell,20,1%
/RadioCommunication,10,0.5%,RadioComm,RadioCommunicationComponent
/Reactor,25,2%,,ReactorComponent
/SmallTube,50,3%
/SolarCell,20,0.1%
/SteelPlate,150,40%
/Superconductor,10,1%
/Thrust,15,5%,,ThrustComponent
/Canvas,5,0.01%

GasContainerObject/
/HydrogenBottle

Ingot/
/Cobalt,50,3.5%
/Gold,5,0.2%
/Iron,200,88%
/Magnesium,5,0.1%
/Nickel,30,1.5%
/Platinum,5,0.1%
/Silicon,50,2%
/Silver,20,1%
/Stone,50,2.5%
/Uranium,1,0.1%

Ore/
/Cobalt
/Gold
/Ice
/Iron
/Magnesium
/Nickel
/Platinum
/Scrap
/Silicon
/Silver
/Stone
/Uranium

OxygenContainerObject/
/OxygenBottle

PhysicalGunObject/
/AngleGrinderItem,,,,AngleGrinder
/AngleGrinder2Item,,,,AngleGrinder2
/AngleGrinder3Item,,,,AngleGrinder3
/AngleGrinder4Item,,,,AngleGrinder4
/AutomaticRifleItem,,,AutomaticRifle,AutomaticRifle
/HandDrillItem,,,,HandDrill
/HandDrill2Item,,,,HandDrill2
/HandDrill3Item,,,,HandDrill3
/HandDrill4Item,,,,HandDrill4
/PreciseAutomaticRifleItem,,,PreciseAutomaticRifle,PreciseAutomaticRifle
/RapidFireAutomaticRifleItem,,,RapidFireAutomaticRifle,RapidFireAutomaticRifle
/UltimateAutomaticRifleItem,,,UltimateAutomaticRifle,UltimateAutomaticRifle
/WelderItem,,,,Welder
/Welder2Item,,,,Welder2
/Welder3Item,,,,Welder3
/Welder4Item,,,,Welder4
";

        // Item types which may have quantities which are not whole numbers.
        static readonly HashSet<string> FRACTIONAL_TYPES = new HashSet<string> { "INGOT", "ORE" };

        // Ore subtypes which refine into Ingots with a different subtype name, or
        // which cannot be refined at all (if set to "").
        static readonly Dictionary<string, string> ORE_PRODUCT = new Dictionary<string, string>
        {
            // vanilla products
            { "ICE", "" }, { "ORGANIC", "" }, { "SCRAP", "IRON" },

            // better stone products
            // http://steamcommunity.com/sharedfiles/filedetails/?id=406244471
            {"DENSE IRON", "IRON"}, {"ICY IRON", "IRON"}, {"HEAZLEWOODITE", "NICKEL"}, {"CATTIERITE", "COBALT"}, {"PYRITE", "GOLD"},
            {"TAENITE", "NICKEL"}, {"COHENITE", "COBALT"}, {"KAMACITE", "NICKEL"}, {"GLAUCODOT", "COBALT"}, {"ELECTRUM", "GOLD"},
            {"PORPHYRY", "GOLD"}, {"SPERRYLITE", "PLATINUM"}, {"NIGGLIITE", "PLATINUM"}, {"GALENA", "SILVER"}, {"CHLORARGYRITE", "SILVER"},
            {"COOPERITE", "PLATINUM"}, {"PETZITE", "SILVER"}, {"HAPKEITE", "SILICON"}, {"DOLOMITE", "MAGNESIUM"}, {"SINOITE", "SILICON"},
            {"OLIVINE", "MAGNESIUM"}, {"QUARTZ", "SILICON"}, {"AKIMOTOITE", "MAGNESIUM"}, {"WADSLEYITE", "MAGNESIUM"}, {"CARNOTITE", "URANIUM"},
            {"AUTUNITE", "URANIUM"}, {"URANIAURITE", "GOLD"}
        };

        // Block types/subtypes which restrict item types/subtypes from their first
        // inventory. Missing or "*" subtype indicates all subtypes of the given type.
        const string DEFAULT_RESTRICTIONS =
        MOB + "Assembler:AmmoMagazine,Component,GasContainerObject,Ore,OxygenContainerObject,PhysicalGunObject\n" +
        MOB + "InteriorTurret:AmmoMagazine/Missile200mm,AmmoMagazine/NATO_25x184mm," + NON_AMMO +
        MOB + "LargeGatlingTurret:AmmoMagazine/Missile200mm,AmmoMagazine/NATO_5p56x45mm," + NON_AMMO +
        MOB + "LargeMissileTurret:AmmoMagazine/NATO_25x184mm,AmmoMagazine/NATO_5p56x45mm," + NON_AMMO +
        MOB + "OxygenGenerator:AmmoMagazine,Component,Ingot,Ore/Cobalt,Ore/Gold,Ore/Iron,Ore/Magnesium,Ore/Nickel,Ore/Organic,Ore/Platinum,Ore/Scrap,Ore/Silicon,Ore/Silver,Ore/Stone,Ore/Uranium,PhysicalGunObject\n" +
        MOB + "OxygenTank:AmmoMagazine,Component,GasContainerObject,Ingot,Ore,PhysicalGunObject\n" +
        MOB + "OxygenTank/LargeHydrogenTank:AmmoMagazine,Component,Ingot,Ore,OxygenContainerObject,PhysicalGunObject\n" +
        MOB + "OxygenTank/SmallHydrogenTank:AmmoMagazine,Component,Ingot,Ore,OxygenContainerObject,PhysicalGunObject\n" +
        MOB + "Reactor:AmmoMagazine,Component,GasContainerObject,Ingot/Cobalt,Ingot/Gold,Ingot/Iron,Ingot/Magnesium,Ingot/Nickel,Ingot/Platinum,Ingot/Scrap,Ingot/Silicon,Ingot/Silver,Ingot/Stone,Ore,OxygenContainerObject,PhysicalGunObject\n" +
        MOB + "Refinery:AmmoMagazine,Component,GasContainerObject,Ingot,Ore/Ice,Ore/Organic,OxygenContainerObject,PhysicalGunObject\n" +
        MOB + "Refinery/Blast Furnace:AmmoMagazine,Component,GasContainerObject,Ingot,Ore/Gold,Ore/Ice,Ore/Magnesium,Ore/Organic,Ore/Platinum,Ore/Silicon,Ore/Silver,Ore/Stone,Ore/Uranium,OxygenContainerObject,PhysicalGunObject\n" +
        MOB + "SmallGatlingGun:AmmoMagazine/Missile200mm,AmmoMagazine/NATO_5p56x45mm," + NON_AMMO +
        MOB + "SmallMissileLauncher:AmmoMagazine/NATO_25x184mm,AmmoMagazine/NATO_5p56x45mm," + NON_AMMO +
        MOB + "SmallMissileLauncherReload:AmmoMagazine/NATO_25x184mm,AmmoMagazine/NATO_5p56x45mm," + NON_AMMO +
        MOB + "Parachute:Ingot,Ore,OxygenContainerObject,PhysicalGunObject,AmmoMagazine,GasContainerObject,Component/Construction,Component/MetalGrid,Component/InteriorPlate,Component/SteelPlate,Component/Girder,Component/SmallTube,Component/LargeTube,Component/Motor,Component/Display,Component/BulletproofGlass,Component/Superconductor,Component/Computer,Component/Reactor,Component/Thrust,Component/GravityGenerator,Component/Medical,Component/RadioCommunication,Component/Detector,Component/Explosives,Component/Scrap,Component/SolarCell,Component/PowerCell"
        ;

        // =================================================
        //                 SCRIPT INTERNALS
        //
        //            Do not edit anything below
        // =================================================
        const string MOB = "MyObjectBuilder_";
        const string NON_AMMO = "Component,GasContainerObject,Ingot,Ore,OxygenContainerObject,PhysicalGunObject\n";
        /*m*/
        #region Fields

        #region Version

        // current script version
        const int VERSION_MAJOR = 1, VERSION_MINOR = 7, VERSION_REVISION = 7;
        /// <summary>
        /// Current script update time.
        /// </summary>
        const string VERSION_UPDATE = "2019-04-07";
        /// <summary>
        /// A formatted string of the script version.
        /// </summary>
        readonly string VERSION_NICE_TEXT = string.Format("v{0}.{1}.{2} ({3})", VERSION_MAJOR, VERSION_MINOR, VERSION_REVISION, VERSION_UPDATE);

        #endregion

        #region Format Strings

        /// <summary>
        /// The format for the text to echo at the start of each call.
        /// </summary>
        const string FORMAT_TIM_UPDATE_TEXT = "Taleden's Inventory Manager\n{0}\nLast run: #{{0}} at {{1}}";
        /// <summary>
        /// The format string for building the tag parser.
        /// {0}: tag open.
        /// {1}: tag close.
        /// {2}: tag prefix.
        /// </summary>
        const string FORMAT_TAG_REGEX_BASE_PREFIX = @"{0} *{2}(|[ ,]+[^{1}]*){1}";
        /// <summary>
        /// The format string for building the tag parser.
        /// {0}: tag open.
        /// {2}: tag close.
        /// </summary>
        const string FORMAT_TAG_REGEX_BASE_NO_PREFIX = @"{0}([^{1}]*){1}";

        #endregion

        #region Arguments

        #region Defaults

        const bool DEFAULT_ARG_REWRITE_TAGS = true;
        const bool DEFAULT_ARG_QUOTA_STABLE = true;
        const char DEFAULT_ARG_TAG_OPEN = '[';
        const char DEFAULT_ARG_TAG_CLOSE = ']';
        const string DEFAULT_ARG_TAG_PREFIX = "TIM";
        const bool DEFAULT_ARG_SCAN_COLLECTORS = false;
        const bool DEFAULT_ARG_SCAN_DRILLS = false;
        const bool DEFAULT_ARG_SCAN_GRINDERS = false;
        const bool DEFAULT_ARG_SCAN_WELDERS = false;

        #endregion

        #region Actual

        /// <summary>
        /// Whether to rewrite TIM tags.
        /// </summary>
        bool argRewriteTags;

        bool argQuotaStable;
        /// <summary>
        /// The opening char for TIM tags.
        /// </summary>
        char argTagOpen;
        /// <summary>
        /// The closing cahr for TIM tags.
        /// </summary>
        char argTagClose;
        /// <summary>
        /// The prefix string for TIM tags.
        /// </summary>
        string argTagPrefix;
        /// <summary>
        /// Whether to scan collectors.
        /// </summary>
        bool argScanCollectors;
        /// <summary>
        /// Whether to scan drills.
        /// </summary>
        bool argScanDrills;
        /// <summary>
        /// Whether to scan grinders.
        /// </summary>
        bool argScanGrinders;
        /// <summary>
        /// Whether to scan welders.
        /// </summary>
        bool argScanWelders;
        /// <summary>
        /// Stores the complete arguments that were last processed to
        /// allow checking if they have changed. This causes the arguments
        /// to only be processed if the user has edited them.
        /// </summary>
        string completeArguments;

        #endregion

        #region Handling

        /// <summary>
        /// The regex used to parse each line of the arguments.
        /// </summary>
        const string ARGUMENT_PARSE_REGEX = @"^([^=\n]*)(?:=([^=\n]*))?$";
        /// <summary>
        /// The regex used to parse each line of the arguments.
        /// </summary>
        readonly System.Text.RegularExpressions.Regex argParseRegex = new System.Text.RegularExpressions.Regex(
            ARGUMENT_PARSE_REGEX,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline |
            System.Text.RegularExpressions.RegexOptions.Compiled);
        /// <summary>
        /// The valid debug argument values.
        /// </summary>
        readonly string[] argValidDebugValues = { "quotas", "sorting", "refineries", "assemblers" };

        #endregion

        #endregion

        #region Helpers

        const StringComparison OIC = StringComparison.OrdinalIgnoreCase;
        const StringSplitOptions REE = StringSplitOptions.RemoveEmptyEntries;
        static readonly char[] SPACE = { ' ', '\t', '\u00AD' }, COLON = { ':' }, NEWLINE = { '\r', '\n' }, SPACECOMMA = { ' ', '\t', '\u00AD', ',' };
        /// <summary>
        /// The <c>string.Format</c> delegate.
        /// Used for the shortand version.
        /// </summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <returns>
        /// A copy of format in which the format items have been replaced by the string representation
        /// of the corresponding objects in args.
        /// </returns>
        // the delegate will go here after minifier is fixed
        /// <summary>
        /// A shorthand for the <c>string.Format</c> function.
        /// </summary>
        // the format function will go here

        #endregion

        #region Block & Type/SubType Information

        /// <summary>
        /// The items that are not allowed in the given block.
        /// Block Type -> block SubType -> Type -> [SubType].
        /// </summary>
        static Dictionary<string, Dictionary<string, Dictionary<string, HashSet<string>>>> blockSubTypeRestrictions = new Dictionary<string, Dictionary<string, Dictionary<string, HashSet<string>>>>();
        static List<string> types = new List<string>();
        static Dictionary<string, string> typeLabel = new Dictionary<string, string>();
        static Dictionary<string, List<string>> typeSubs = new Dictionary<string, List<string>>();
        static Dictionary<string, long> typeAmount = new Dictionary<string, long>();
        static List<string> subs = new List<string>();
        static Dictionary<string, string> subLabel = new Dictionary<string, string>();
        static Dictionary<string, List<string>> subTypes = new Dictionary<string, List<string>>();
        static Dictionary<string, Dictionary<string, InventoryItemData>> typeSubData = new Dictionary<string, Dictionary<string, InventoryItemData>>();
        static Dictionary<MyDefinitionId, ItemId> blueprintItem = new Dictionary<MyDefinitionId, ItemId>();

        #endregion

        #region Script state & storage

        /// <summary>
        /// The header for the statistics panels.
        /// </summary>
        string panelStatsHeader = "";
        /// <summary>
        /// The logs for each cycle.
        /// </summary>
        string[] statsLog = new string[12];
        /// <summary>
        /// The time we started the last cycle at.
        /// If <see cref="USE_REAL_TIME"/> is <c>true</c>, then it is also used to track
        /// when the script should next update
        /// </summary>
        DateTime currentCycleStartTime;
        /// <summary>
        /// The time to wait before starting the next cycle.
        /// Only used if <see cref="USE_REAL_TIME"/> is <c>true</c>.
        /// </summary>
        TimeSpan cycleUpdateWaitTime = new TimeSpan(0, 0, 0, 0, UPDATE_REAL_TIME);
        /// <summary>
        /// The total number of calls this script has had since compilation.
        /// </summary>
        long totalCallCount = 0;
        /// <summary>
        /// The number of items transfers this call.
        /// </summary>
        int numberTransfers;
        /// <summary>
        /// The number of refineries being managed this call.
        /// </summary>
        int numberRefineres;
        /// <summary>
        /// The number of assemblers being managed this call.
        /// </summary>
        int numberAssemblers;
        /// <summary>
        /// The current step in the TIM process cycle.
        /// </summary>
        int processStep = 0;
        /// <summary>
        /// All of the process steps that TIM will need to take,
        /// </summary>
        readonly Action[] processSteps;
        /// <summary>
        /// Regex for testing for whether a block has a TIM tag.
        /// </summary>
        System.Text.RegularExpressions.Regex tagRegex = null;
        /// <summary>
        /// Whether a new item (e.g. from a mod) has been found.
        /// Used to 
        /// </summary>
        static bool foundNewItem = false;
        /// <summary>
        /// The text to echo at the start of each call.
        /// </summary>
        string timUpdateText;
        /// <summary>
        /// Stores the output of Echo so we can effectively ignore some calls
        /// without overwriting it.
        /// </summary>
        StringBuilder echoOutput = new StringBuilder();

        /// <summary>
        /// The set of all docked grid (including the current one).
        /// </summary>
        HashSet<IMyCubeGrid> dockedgrids = new HashSet<IMyCubeGrid>();
        Dictionary<int, Dictionary<string, Dictionary<string, Dictionary<IMyInventory, long>>>> priTypeSubInvenRequest = new Dictionary<int, Dictionary<string, Dictionary<string, Dictionary<IMyInventory, long>>>>();
        Dictionary<IMyTextPanel, int> qpanelPriority = new Dictionary<IMyTextPanel, int>();
        Dictionary<IMyTextPanel, List<string>> qpanelTypes = new Dictionary<IMyTextPanel, List<string>>();
        Dictionary<IMyTextPanel, List<string>> ipanelTypes = new Dictionary<IMyTextPanel, List<string>>();
        List<IMyTextPanel> statusPanels = new List<IMyTextPanel>();
        List<IMyTextPanel> debugPanels = new List<IMyTextPanel>();
        HashSet<string> debugLogic = new HashSet<string>();
        List<string> debugText = new List<string>();
        Dictionary<IMyTerminalBlock, System.Text.RegularExpressions.Match> blockGtag = new Dictionary<IMyTerminalBlock, System.Text.RegularExpressions.Match>();
        Dictionary<IMyTerminalBlock, System.Text.RegularExpressions.Match> blockTag = new Dictionary<IMyTerminalBlock, System.Text.RegularExpressions.Match>();
        HashSet<IMyInventory> invenLocked = new HashSet<IMyInventory>();
        HashSet<IMyInventory> invenHidden = new HashSet<IMyInventory>();
        Dictionary<IMyRefinery, HashSet<string>> refineryOres = new Dictionary<IMyRefinery, HashSet<string>>();
        Dictionary<IMyAssembler, HashSet<ItemId>> assemblerItems = new Dictionary<IMyAssembler, HashSet<ItemId>>();
        Dictionary<IMyFunctionalBlock, ProducerWork> producerWork = new Dictionary<IMyFunctionalBlock, ProducerWork>();
        Dictionary<IMyFunctionalBlock, int> producerJam = new Dictionary<IMyFunctionalBlock, int>();
        Dictionary<IMyTextPanel, Pair> panelSpan = new Dictionary<IMyTextPanel, Pair>();
        Dictionary<IMyTerminalBlock, HashSet<IMyTerminalBlock>> blockErrors = new Dictionary<IMyTerminalBlock, HashSet<IMyTerminalBlock>>();

        #endregion

        #endregion

        #region Helper Methods

        /// <summary>
        /// A wrapper for the <see cref="Echo"/> function that adds the log to the stored log.
        /// This allows the log to be remembered and re-outputted without extra work.
        /// </summary>
        public Action<string> EchoR;

        #endregion

        #region Properties

        /// <summary>
        /// The length of time we have been executing for.
        /// Measured in milliseconds.
        /// </summary>
        int ExecutionTime
        {
            get { return (int)((DateTime.Now - currentCycleStartTime).TotalMilliseconds + 0.5); }
        }

        /// <summary>
        /// The current percent load of the call.
        /// </summary>
        double ExecutionLoad
        {
            get { return Runtime.CurrentInstructionCount / Runtime.MaxInstructionCount; }
        }        

        #endregion

        #region Custom Exceptions

        /// <summary>
        /// Thrown when the script should abort all execution.
        /// If caught, then <c>processStep</c> should be reset to 0.
        /// </summary>
        class IgnoreExecutionException : Exception
        {
        }

        /// <summary>
        /// Thrown when we detect that we have taken up too much processing time
        /// and need to put off the rest of the exection until the next call.
        /// </summary>
        class PutOffExecutionException : Exception
        {
        }

        #endregion

        #region Data Structures

        /// <summary>
        /// Stores a pair of ints.
        /// </summary>
        struct Pair
        {
            public int A, B;

            public Pair(int a, int b)
            {
                A = a;
                B = b;
            }
        }

        /// <summary>
        /// Stores an item's ID.
        /// </summary>
        struct ItemId
        {
            public string type, subType;

            public ItemId(string t, string s)
            {
                type = t;
                subType = s;
            }
        }

        /// <summary>
        /// Stores the work information for an item in a producer block.
        /// </summary>
        struct ProducerWork
        {
            public ItemId item;
            public double quantity;

            public ProducerWork(ItemId i, double q)
            {
                item = i;
                quantity = q;
            }
        }

        #endregion

        #region Data Classes

        class InventoryItemData
        {
            public string subType, label;
            public MyDefinitionId blueprint;
            public long amount, avail, locked, quota, minimum;
            public float ratio;
            public int qpriority, hold, jam;
            public Dictionary<IMyInventory, long> invenTotal;
            public Dictionary<IMyInventory, int> invenSlot;
            public HashSet<IMyFunctionalBlock> producers;
            public Dictionary<string, double> prdSpeed;

            /// <summary>
            /// Initialises the item with base data.
            /// </summary>
            /// <param name="itemType">Item Type ID.</param>
            /// <param name="itemSubType">Item SubType ID.</param>
            /// <param name="minimum"></param>
            /// <param name="ratio"></param>
            /// <param name="label"></param>
            /// <param name="blueprint"></param>
            public static void InitItem(string itemType, string itemSubType, long minimum = 0L, float ratio = 0.0f, string label = "", string blueprint = "")
            {
                string itypelabel = itemType, isublabel = itemSubType;
                itemType = itemType.ToUpper();
                itemSubType = itemSubType.ToUpper();

                // new type?
                if (!typeSubs.ContainsKey(itemType))
                {
                    types.Add(itemType);
                    typeLabel[itemType] = itypelabel;
                    typeSubs[itemType] = new List<string>();
                    typeAmount[itemType] = 0L;
                    typeSubData[itemType] = new Dictionary<string, InventoryItemData>();
                }

                // new subtype?
                if (!subTypes.ContainsKey(itemSubType))
                {
                    subs.Add(itemSubType);
                    subLabel[itemSubType] = isublabel;
                    subTypes[itemSubType] = new List<string>();
                }

                // new type/subtype pair?
                if (!typeSubData[itemType].ContainsKey(itemSubType))
                {
                    foundNewItem = true;
                    typeSubs[itemType].Add(itemSubType);
                    subTypes[itemSubType].Add(itemType);
                    typeSubData[itemType][itemSubType] = new InventoryItemData(itemSubType, minimum, ratio, (label == "") ? isublabel : label, (blueprint == "") ? isublabel : blueprint);
                    if (blueprint != null)
                        blueprintItem[typeSubData[itemType][itemSubType].blueprint] = new ItemId(itemType, itemSubType);
                }
            }

            private InventoryItemData(string isub, long minimum, float ratio, string label, string blueprint)
            {
                subType = isub;
                this.label = label;
                this.blueprint = (blueprint == null) ? default(MyDefinitionId) : MyDefinitionId.Parse("MyObjectBuilder_BlueprintDefinition/" + blueprint);
                amount = avail = locked = quota = 0L;
                this.minimum = (long)(minimum * 1000000.0 + 0.5);
                this.ratio = (ratio / 100.0f);
                qpriority = -1;
                hold = jam = 0;
                invenTotal = new Dictionary<IMyInventory, long>();
                invenSlot = new Dictionary<IMyInventory, int>();
                producers = new HashSet<IMyFunctionalBlock>();
                prdSpeed = new Dictionary<string, double>();
            }
        }

        #endregion

        #region Entry Points

        public Program()
        {
            // init echo wrapper
            EchoR = log =>
            {
                echoOutput.AppendLine(log);
                Echo(log);
            };

            // initialise the process steps we will need to do
            processSteps = new Action[]
            {
                ProcessStepProcessArgs,           // 0:  always process arguments first to handle changes
                ProcessStepScanGrids,             // 1:  scan grids next to find out if there is another TIM in the terminal system
                ProcessStepStandbyCheck,          // 2:  detect if another TIM should run instead and if we should be backup
                ProcessStepInventoryScan,         // 3:  do the inventory scanning
                ProcessStepParseTags,             // 4:  parse the tags of the blocks we found
                ProcessStepAmountAdjustment,      // 5:  adjust item amounts based on what's available
                ProcessStepQuotaPanels,           // 6:  handle quota panels
                ProcessStepLimitedItemRequests,   // 7:  handle limited item requests
                ProcessStepManageRefineries,      // 8:  handle all refineries we need to
                ProcessStepUnlimitedItemRequests, // 9:  handle unlimited item requests
                ProcessStepManageAssemblers,      // 10: handle all assemblers we need to
                ProcessStepScanProduction,        // 11: scan all production blocks and handle them
                ProcessStepUpdateInventoryPanels, // 12: update all inventory panels
            };

            // initialize panel data
            int unused;
            ScreenFormatter.Init();
            panelStatsHeader = (
                "Taleden's Inventory Manager\n" +
                VERSION_NICE_TEXT + "\n\n" +
                ScreenFormatter.Format("Run", 80, out unused, 1) +
                ScreenFormatter.Format("F-Step", 125 + unused, out unused, 1) +
                ScreenFormatter.Format("Time", 145 + unused, out unused, 1) +
                ScreenFormatter.Format("Load", 105 + unused, out unused, 1) +
                ScreenFormatter.Format("S", 65 + unused, out unused, 1) +
                ScreenFormatter.Format("R", 65 + unused, out unused, 1) +
                ScreenFormatter.Format("A", 65 + unused, out unused, 1) +
                "\n\n"
            );

            // initialize default items, quotas, labels and blueprints
            // (TIM can also learn new items it sees in inventory)
            InitItems(DEFAULT_ITEMS);

            // initialize block:item restrictions
            // (TIM can also learn new restrictions whenever item transfers fail)
            InitBlockRestrictions(DEFAULT_RESTRICTIONS);

            // Set run frequency
            if (USE_REAL_TIME)
                Runtime.UpdateFrequency = UpdateFrequency.Update1;
            else
                Runtime.UpdateFrequency = UPDATE_FREQUENCY;

            // echo compilation statement
            EchoR("Compiled TIM " + VERSION_NICE_TEXT);

            // format terminal info text
            timUpdateText = string.Format(FORMAT_TIM_UPDATE_TEXT, VERSION_NICE_TEXT);
        }

        public void Main(string argument)
        {
            // init call
            // do update frequency check
            if (USE_REAL_TIME)
            {
                DateTime n = DateTime.Now;
                if ((n - currentCycleStartTime) >= cycleUpdateWaitTime)
                    currentCycleStartTime = n;
                else
                {
                    Echo(echoOutput.ToString()); // ensure that output is not lost
                    return;
                }
            }
            else
                currentCycleStartTime = DateTime.Now;
            echoOutput.Clear();
            int processStepTmp = processStep;
            bool didAtLeastOneProcess = false;

            // output terminal info
            EchoR(string.Format(timUpdateText, ++totalCallCount, currentCycleStartTime.ToString("h:mm:ss tt")));

            // reset status and debugging data every cycle
            debugText.Clear();
            debugLogic.Clear();
            numberTransfers = numberRefineres = numberAssemblers = 0;

            try
            {
                do
                {
                    debugText.Add(string.Format("> Doing step {0}", processStep));
                    processSteps[processStep]();
                    processStep++;
                    didAtLeastOneProcess = true;
                } while (processStep < processSteps.Length && DoExecutionLimitCheck());
                // if we get here it means we completed all the process steps
                processStep = 0;
            }
            catch (ArgumentException ex)
            {
                EchoR(ex.Message);
                processStep = 0;
                return;
            }
            catch (IgnoreExecutionException)
            {
                processStep = 0;
                return;
            }
            catch (PutOffExecutionException)
            { }
            catch (Exception ex)
            {
                // if the process step threw an exception, make sure we print the info
                // we need to debug it
                string err = "An error occured,\n" +
                    "please give the following information to the developer:\n" +
                    string.Format("Current step on error: {0}\n{1}", processStep, ex.ToString().Replace("\r", ""));
                debugText.Add(err);
                UpdateStatusPanels();
                EchoR(err);
                throw ex;
            }

            // update script status and debug panels on every cycle step
            string msg, stepText;
            int theoryProcessStep = processStep == 0 ? 13 : processStep;
            int exTime = ExecutionTime;
            double exLoad = Math.Round(100.0f * ExecutionLoad, 1);
            int unused = 0;
            statsLog[totalCallCount % statsLog.Length] = (
                ScreenFormatter.Format("" + totalCallCount, 80, out unused, 1) +
                ScreenFormatter.Format((processStep == 0 ? processSteps.Length : processStep) + " / " + processSteps.Length, 125 + unused, out unused, 1, true) +
                ScreenFormatter.Format(exTime + " ms", 145 + unused, out unused, 1) +
                ScreenFormatter.Format(exLoad + "%", 105 + unused, out unused, 1, true) +
                ScreenFormatter.Format("" + numberTransfers, 65 + unused, out unused, 1, true) +
                ScreenFormatter.Format("" + numberRefineres, 65 + unused, out unused, 1, true) +
                ScreenFormatter.Format("" + numberAssemblers, 65 + unused, out unused, 1, true) +
                "\n"
            );
            if (processStep == 0 && processStepTmp == 0 && didAtLeastOneProcess)
                stepText = "all steps";
            else if (processStep == processStepTmp)
                stepText = string.Format("step {0} partially", processStep);
            else if (theoryProcessStep - processStepTmp == 1)
                stepText = string.Format("step {0}", processStepTmp);
            else
                stepText = string.Format("steps {0} to {1}", processStepTmp, theoryProcessStep - 1);
            EchoR(msg = string.Format("Completed {0} in {1}ms, {2}% load ({3} instructions)",
                stepText, exTime, exLoad, Runtime.CurrentInstructionCount));
            debugText.Add(msg);
            UpdateStatusPanels();
        }

        #endregion

        #region Init

        void InitItems(string data)
        {
            string itype = "";
            long minimum;
            float ratio;
            foreach (string line in data.Split(NEWLINE, REE))
            {
                string[] words = (line.Trim() + ",,,,").Split(SPACECOMMA, 6);
                words[0] = words[0].Trim();
                if (words[0].EndsWith("/"))
                {
                    itype = words[0].Substring(0, words[0].Length - 1);
                }
                else if (itype != "" & words[0].StartsWith("/"))
                {
                    long.TryParse(words[1], out minimum);
                    float.TryParse(words[2].Substring(0, (words[2] + "%").IndexOf("%")), out ratio);
                    InventoryItemData.InitItem(itype, words[0].Substring(1), minimum, ratio, words[3].Trim(), (itype == "Ingot" | itype == "Ore") ? null : words[4].Trim());
                }
            }
        }


        void InitBlockRestrictions(string data)
        {
            foreach (string line in data.Split(NEWLINE, REE))
            {
                string[] blockitems = (line + ":").Split(':');
                string[] block = (blockitems[0] + "/*").Split('/');
                foreach (string item in blockitems[1].Split(','))
                {
                    string[] typesub = item.ToUpper().Split('/');
                    AddBlockRestriction(block[0].Trim(SPACE), block[1].Trim(SPACE), typesub[0], ((typesub.Length > 1) ? typesub[1] : null), true);
                }
            }
        }

        #endregion

        #region Runtime

        #region Arguments

        void ProcessScriptArgs()
        {
            // init all args back to default
            argRewriteTags = DEFAULT_ARG_REWRITE_TAGS;
            argTagOpen = DEFAULT_ARG_TAG_OPEN;
            argTagClose = DEFAULT_ARG_TAG_CLOSE;
            argTagPrefix = DEFAULT_ARG_TAG_PREFIX;
            argScanCollectors = DEFAULT_ARG_SCAN_COLLECTORS;
            argScanDrills = DEFAULT_ARG_SCAN_DRILLS;
            argScanGrinders = DEFAULT_ARG_SCAN_GRINDERS;
            argScanWelders = DEFAULT_ARG_SCAN_WELDERS;
            argQuotaStable = DEFAULT_ARG_QUOTA_STABLE;

            string arg, value;
            bool hasValue;
            foreach (System.Text.RegularExpressions.Match match in argParseRegex.Matches(Me.CustomData))
            {
                arg = match.Groups[1].Value.ToLower();
                hasValue = match.Groups[2].Success;
                if (hasValue)
                    value = match.Groups[2].Value.Trim();
                else
                    value = "";
                switch (arg)
                {
                    case "rewrite":
                        if (hasValue)
                            throw new ArgumentException("Argument 'rewrite' does not have a value");
                        argRewriteTags = true;
                        debugText.Add("Tag rewriting enabled");
                        break;
                    case "norewrite":
                        if (hasValue)
                            throw new ArgumentException("Argument 'norewrite' does not have a value");
                        argRewriteTags = false;
                        debugText.Add("Tag rewriting disabled");
                        break;
                    case "tags":
                        if (value.Length != 2)
                            throw new ArgumentException(string.Format("Invalid 'tags=' delimiters '{0}': must be exactly two characters", value));
                        else if (char.ToUpper(value[0]) == char.ToUpper(value[1]))
                            throw new ArgumentException(string.Format("Invalid 'tags=' delimiters '{0}': characters must be different", value));
                        else
                        {
                            argTagOpen = char.ToUpper(value[0]);
                            argTagClose = char.ToUpper(value[1]);
                            debugText.Add(string.Format("Tags are delimited by '{0}' and '{1}", argTagOpen, argTagClose));
                        }
                        break;
                    case "prefix":
                        argTagPrefix = value.ToUpper();
                        if (argTagPrefix == "")
                            debugText.Add("Tag prefix disabled");
                        else
                            debugText.Add(string.Format("Tag prefix is '{0}'", argTagPrefix));
                        break;
                    case "scan":
                        switch (value.ToLower())
                        {
                            case "collectors":
                                argScanCollectors = true;
                                debugText.Add("Enabled scanning of Collectors");
                                break;
                            case "drills":
                                argScanDrills = true;
                                debugText.Add("Enabled scanning of Drills");
                                break;
                            case "grinders":
                                argScanGrinders = true;
                                debugText.Add("Enabled scanning of Grinders");
                                break;
                            case "welders":
                                argScanWelders = true;
                                debugText.Add("Enabled scanning of Welders");
                                break;
                            default:
                                throw new ArgumentException(string.Format("Invalid 'scan=' block type '{0}': must be 'collectors', 'drills', 'grinders' or 'welders'", value));
                        }
                        break;
                    case "quota":
                        switch (value.ToLower())
                        {
                            case "literal":
                                argQuotaStable = false;
                                debugText.Add("Disabled stable dynamic quotas");
                                break;
                            case "stable":
                                argQuotaStable = true;
                                debugText.Add("Enabled stable dynamic quotas");
                                break;
                            default:
                                throw new ArgumentException(string.Format("Invalid 'quota=' mode '{0}': must be 'literal' or 'stable'", value));
                        }
                        break;
                    case "debug":
                        value = value.ToLower();
                        if (argValidDebugValues.Contains(value))
                            debugLogic.Add(value);
                        else
                            throw new ArgumentException(string.Format("Invalid 'debug=' type '{0}': must be 'quotas', 'sorting', 'refineries', or 'assemblers'",
                                    value));
                        break;
                    case "":
                    case "tim_version":
                        break;
                    default:
                        // if an argument is not recognised, abort
                        throw new ArgumentException(string.Format("Unrecognized argument: '{0}'", arg));
                }
            }

            tagRegex = new System.Text.RegularExpressions.Regex(string.Format(
                argTagPrefix != "" ? FORMAT_TAG_REGEX_BASE_PREFIX : FORMAT_TAG_REGEX_BASE_NO_PREFIX, // select regex statement
                System.Text.RegularExpressions.Regex.Escape(argTagOpen.ToString()),
                System.Text.RegularExpressions.Regex.Escape(argTagClose.ToString()),
                System.Text.RegularExpressions.Regex.Escape(argTagPrefix)), // format in args
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);
        }

        #endregion

        #region Process Steps

        // This is where all the steps we need to complete are.
        // At the end of each step, a check will be done to decide
        // whether we should continue the processing or wait till the next
        // call. However, if any step raises a PutOffExecutionException,
        // then we will wait until the next call to complete that step.

        /// <summary>
        /// Processes the block arguments.
        /// </summary>
        /// <returns>Whether the step completed.</returns>
        public void ProcessStepProcessArgs()
        {
            if (Me.CustomData != completeArguments)
            {
                debugText.Add("Arguments changed, re-processing...");
                ProcessScriptArgs();
                completeArguments = Me.CustomData;
            }
        }

        /// <summary>
        /// Scans all the grids and initialises the connections
        /// </summary>
        /// <returns>Whether the step completed.</returns>
        public void ProcessStepScanGrids()
        {
            debugText.Add("Scanning grid connectors...");
            ScanGrids();
        }

        public void ProcessStepStandbyCheck()
        {
            // search for other TIMs
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyProgrammableBlock>(blocks, (IMyTerminalBlock blk) => (blk == Me) | (tagRegex.IsMatch(blk.CustomName) & dockedgrids.Contains(blk.CubeGrid)));

            // check to see if this block is the first available TIM
            int selfIndex = blocks.IndexOf(Me); // current index in search
            int firstAvailableIndex = blocks.FindIndex(block => block.IsFunctional & block.IsWorking); // first available in search

            // update custom name based on current index
            string updatedCustomName = argTagOpen + argTagPrefix + ((blocks.Count > 1) ? (" #" + (selfIndex + 1)) : "") + argTagClose;
            Me.CustomName = tagRegex.IsMatch(Me.CustomName) ? tagRegex.Replace(Me.CustomName, updatedCustomName, 1) : (Me.CustomName + " " + updatedCustomName);

            // if there are other programmable blocks of higher index, then they will execute and we won't
            if (selfIndex != firstAvailableIndex)
            {
                EchoR("TIM #" + (firstAvailableIndex + 1) + " is on duty. Standing by.");
                if (("" + (blocks[firstAvailableIndex] as IMyProgrammableBlock).TerminalRunArgument).Trim() != ("" + Me.TerminalRunArgument).Trim())
                    EchoR("WARNING: Script arguments do not match TIM #" + (firstAvailableIndex + 1) + ".");
                throw new IgnoreExecutionException();
            }
        }

        /// <summary>
        /// Scans all inventories to build what blocks need to be processed.
        /// </summary>
        /// <returns>Whether the step completed.</returns>
        public void ProcessStepInventoryScan()
        {
            debugText.Add("Scanning inventories...");

            // reset everything that we'll check during this step
            foreach (string itype in types)
            {
                typeAmount[itype] = 0;
                foreach (InventoryItemData data in typeSubData[itype].Values)
                {
                    data.amount = 0L;
                    data.avail = 0L;
                    data.locked = 0L;
                    data.invenTotal.Clear();
                    data.invenSlot.Clear();
                }
            }
            blockTag.Clear();
            blockGtag.Clear();
            invenLocked.Clear();
            invenHidden.Clear();

            // scan inventories
            ScanGroups();
            ScanBlocks<IMyAssembler>();
            ScanBlocks<IMyCargoContainer>();
            if (argScanCollectors)
                ScanBlocks<IMyCollector>();
            ScanBlocks<IMyGasGenerator>();
            ScanBlocks<IMyGasTank>();
            ScanBlocks<IMyOxygenFarm>(); // scan oxygen farm to allow nanite support
            ScanBlocks<IMyReactor>();
            ScanBlocks<IMyRefinery>();
            ScanBlocks<IMyShipConnector>();
            ScanBlocks<IMyShipController>();
            if (argScanDrills)
                ScanBlocks<IMyShipDrill>();
            if (argScanGrinders)
                ScanBlocks<IMyShipGrinder>();
            if (argScanWelders)
                ScanBlocks<IMyShipWelder>();
            ScanBlocks<IMyTextPanel>();
            ScanBlocks<IMyUserControllableGun>();
            ScanBlocks<IMyParachute>();

            // if we found any new item type/subtypes, re-sort the lists
            if (foundNewItem)
            {
                foundNewItem = false;
                types.Sort();
                foreach (string itype in types)
                    typeSubs[itype].Sort();
                subs.Sort();
                foreach (string isub in subs)
                    subTypes[isub].Sort();
            }
        }

        /// <summary>
        /// Parses all found block tags.
        /// </summary>
        /// <returns>Whether the step completed.</returns>
        public void ProcessStepParseTags()
        {
            debugText.Add("Scanning tags...");

            // reset everything that we'll check during this step
            foreach (string itype in types)
            {
                foreach (InventoryItemData data in typeSubData[itype].Values)
                {
                    data.qpriority = -1;
                    data.quota = 0L;
                    data.producers.Clear();
                }
            }
            qpanelPriority.Clear();
            qpanelTypes.Clear();
            ipanelTypes.Clear();
            priTypeSubInvenRequest.Clear();
            statusPanels.Clear();
            debugPanels.Clear();
            refineryOres.Clear();
            assemblerItems.Clear();
            panelSpan.Clear();

            // parse tags
            ParseBlockTags();
        }

        /// <summary>
        /// Adjusts the tracked amounts of items in inventories.
        /// </summary>
        /// <returns>Whether the step completed.</returns>
        public void ProcessStepAmountAdjustment()
        {
            debugText.Add("Adjusting tallies...");
            AdjustAmounts();
        }

        /// <summary>
        /// Processes quota panels.
        /// </summary>
        /// <returns>Whether the step completed.</returns>
        public void ProcessStepQuotaPanels()
        {
            debugText.Add("Scanning quota panels...");
            ProcessQuotaPanels(argQuotaStable);
        }

        /// <summary>
        /// Processes the limited item allocations.
        /// </summary>
        /// <returns>Whether the step completed.</returns>
        public void ProcessStepLimitedItemRequests()
        {
            debugText.Add("Processing limited item requests...");
            AllocateItems(true); // limited requests
        }

        /// <summary>
        /// Manages handled refineries.
        /// </summary>
        /// <returns>Whether the step completed.</returns>
        public void ProcessStepManageRefineries()
        {
            debugText.Add("Managing refineries...");
            ManageRefineries();
        }

        /// <summary>
        /// Scans all production blocks.
        /// </summary>
        /// <returns>Whether the step completed.</returns>
        public void ProcessStepScanProduction()
        {
            debugText.Add("Scanning production...");
            ScanProduction();
        }

        /// <summary>
        /// Process unlimited item requests using the remaining items.
        /// </summary>
        /// <returns>Whether the step completed.</returns>
        public void ProcessStepUnlimitedItemRequests()
        {
            debugText.Add("Processing remaining item requests...");
            AllocateItems(false); // unlimited requests
        }

        /// <summary>
        /// Manages handled assemblers.
        /// </summary>
        /// <returns>Whether the step completed.</returns>
        public void ProcessStepManageAssemblers()
        {
            debugText.Add("Managing assemblers...");
            ManageAssemblers();
        }

        /// <summary>
        /// Updates all inventory panels.
        /// </summary>
        /// <returns>Whether the step completed.</returns>
        public void ProcessStepUpdateInventoryPanels()
        {
            debugText.Add("Updating inventory panels...");
            UpdateInventoryPanels();
        }

        #endregion

        #endregion

        #region Util

        /// <summary>
        /// Checks if the current call has exceeded the maximum execution limit.
        /// If it has, then it will raise a <see cref="PutOffExecutionException:T"/>.
        /// </summary>
        /// <returns>True.</returns>
        /// <remarks>This methods returns true by default to allow use in the while check.</remarks>
        bool DoExecutionLimitCheck()
        {
            if (ExecutionTime > MAX_RUN_TIME || ExecutionLoad > MAX_LOAD)
                throw new PutOffExecutionException();
            return true;
        }

        void AddBlockRestriction(string btype, string bsub, string itype, string isub, bool init = false)
        {
            Dictionary<string, Dictionary<string, HashSet<string>>> bsubItypeRestr;
            Dictionary<string, HashSet<string>> itypeRestr;
            HashSet<string> restr;

            if (!blockSubTypeRestrictions.TryGetValue(btype.ToUpper(), out bsubItypeRestr))
                blockSubTypeRestrictions[btype.ToUpper()] = bsubItypeRestr = new Dictionary<string, Dictionary<string, HashSet<string>>> { { "*", new Dictionary<string, HashSet<string>>() } };
            if (!bsubItypeRestr.TryGetValue(bsub.ToUpper(), out itypeRestr))
            {
                bsubItypeRestr[bsub.ToUpper()] = itypeRestr = new Dictionary<string, HashSet<string>>();
                if (bsub != "*" & !init)
                {
                    foreach (KeyValuePair<string, HashSet<string>> pair in bsubItypeRestr["*"])
                        itypeRestr[pair.Key] = ((pair.Value != null) ? (new HashSet<string>(pair.Value)) : null);
                }
            }
            if (isub == null | isub == "*")
            {
                itypeRestr[itype] = null;
            }
            else
            {
                (itypeRestr.TryGetValue(itype, out restr) ? restr : (itypeRestr[itype] = new HashSet<string>())).Add(isub);
            }
            if (!init) debugText.Add(btype + "/" + bsub + " does not accept " + typeLabel[itype] + "/" + subLabel[isub]);
        }

        bool BlockAcceptsTypeSub(IMyCubeBlock block, string itype, string isub)
        {
            Dictionary<string, Dictionary<string, HashSet<string>>> bsubItypeRestr;
            Dictionary<string, HashSet<string>> itypeRestr;
            HashSet<string> restr;

            if (blockSubTypeRestrictions.TryGetValue(block.BlockDefinition.TypeIdString.ToUpper(), out bsubItypeRestr))
            {
                bsubItypeRestr.TryGetValue(block.BlockDefinition.SubtypeName.ToUpper(), out itypeRestr);
                if ((itypeRestr ?? bsubItypeRestr["*"]).TryGetValue(itype, out restr))
                    return !(restr == null || restr.Contains(isub));
            }
            return true;
        }

        HashSet<string> GetBlockAcceptedSubs(IMyCubeBlock block, string itype, HashSet<string> mysubs = null)
        {
            Dictionary<string, Dictionary<string, HashSet<string>>> bsubItypeRestr;
            Dictionary<string, HashSet<string>> itypeRestr;
            HashSet<string> restr;

            mysubs = mysubs ?? new HashSet<string>(typeSubs[itype]);
            if (blockSubTypeRestrictions.TryGetValue(block.BlockDefinition.TypeIdString.ToUpper(), out bsubItypeRestr))
            {
                bsubItypeRestr.TryGetValue(block.BlockDefinition.SubtypeName.ToUpper(), out itypeRestr);
                if ((itypeRestr ?? bsubItypeRestr["*"]).TryGetValue(itype, out restr))
                    mysubs.ExceptWith(restr ?? mysubs);
            }
            return mysubs;
        }

        string GetBlockImpliedType(IMyCubeBlock block, string isub)
        {
            string rtype;
            rtype = null;
            foreach (string itype in subTypes[isub])
            {
                if (BlockAcceptsTypeSub(block, itype, isub))
                {
                    if (rtype != null)
                        return null;
                    rtype = itype;
                }
            }
            return rtype;
        }

        string GetShorthand(long amount)
        {
            long scale;
            if (amount <= 0L)
                return "0";
            if (amount < 10000L)
                return "< 0.01";
            if (amount >= 100000000000000L)
                return "" + (amount / 1000000000000L) + " M";
            scale = (long)Math.Pow(10.0, Math.Floor(Math.Log10(amount)) - 2.0);
            amount = (long)((double)amount / scale + 0.5) * scale;
            if (amount < 1000000000L)
                return (amount / 1e6).ToString("0.##");
            if (amount < 1000000000000L)
                return (amount / 1e9).ToString("0.##") + " K";
            return (amount / 1e12).ToString("0.##") + " M";
        }

        #endregion

        #region Scanning

        #region Grid Scanning

        void ScanGrids()
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            IMyCubeGrid g1, g2;
            Dictionary<IMyCubeGrid, HashSet<IMyCubeGrid>> gridLinks = new Dictionary<IMyCubeGrid, HashSet<IMyCubeGrid>>();
            Dictionary<IMyCubeGrid, int> gridShip = new Dictionary<IMyCubeGrid, int>();
            List<HashSet<IMyCubeGrid>> shipGrids = new List<HashSet<IMyCubeGrid>>();
            List<string> shipName = new List<string>();
            HashSet<IMyCubeGrid> grids;
            List<IMyCubeGrid> gqueue = new List<IMyCubeGrid>(); // actual Queue lacks AddRange
            int q, s1, s2;
            IMyShipConnector conn2;
            HashSet<string> tags1 = new HashSet<string>();
            HashSet<string> tags2 = new HashSet<string>();
            System.Text.RegularExpressions.Match match;
            Dictionary<int, Dictionary<int, List<string>>> shipShipDocks = new Dictionary<int, Dictionary<int, List<string>>>();
            Dictionary<int, List<string>> shipDocks;
            List<string> docks;
            HashSet<int> ships = new HashSet<int>();
            Queue<int> squeue = new Queue<int>();

            // find mechanical links
            GridTerminalSystem.GetBlocksOfType<IMyMechanicalConnectionBlock>(blocks);
            foreach (IMyTerminalBlock block in blocks)
            {
                g1 = block.CubeGrid;
                g2 = (block as IMyMechanicalConnectionBlock).TopGrid;
                if (g2 == null)
                    continue;
                (gridLinks.TryGetValue(g1, out grids) ? grids : (gridLinks[g1] = new HashSet<IMyCubeGrid>())).Add(g2);
                (gridLinks.TryGetValue(g2, out grids) ? grids : (gridLinks[g2] = new HashSet<IMyCubeGrid>())).Add(g1);
            }

            // each connected component of mechanical links is a "ship"
            foreach (IMyCubeGrid grid in gridLinks.Keys)
            {
                if (!gridShip.ContainsKey(grid))
                {
                    s1 = (grid.Max - grid.Min + Vector3I.One).Size;
                    g1 = grid;
                    gridShip[grid] = shipGrids.Count;
                    grids = new HashSet<IMyCubeGrid> { grid };
                    gqueue.Clear();
                    gqueue.AddRange(gridLinks[grid]);
                    for (q = 0; q < gqueue.Count; q++)
                    {
                        g2 = gqueue[q];
                        if (!grids.Add(g2))
                            continue;
                        s2 = (g2.Max - g2.Min + Vector3I.One).Size;
                        g1 = (s2 > s1) ? g2 : g1;
                        s1 = (s2 > s1) ? s2 : s1;
                        gridShip[g2] = shipGrids.Count;
                        gqueue.AddRange(gridLinks[g2].Except(grids));
                    }
                    shipGrids.Add(grids);
                    shipName.Add(g1.CustomName);
                }
            }

            // connectors require at least one shared dock tag, or no tags on either
            GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(blocks);
            foreach (IMyTerminalBlock block in blocks)
            {
                conn2 = (block as IMyShipConnector).OtherConnector;
                if (conn2 != null && (block.EntityId < conn2.EntityId & (block as IMyShipConnector).Status == MyShipConnectorStatus.Connected))
                {
                    tags1.Clear();
                    tags2.Clear();
                    if ((match = tagRegex.Match(block.CustomName)).Success)
                    {
                        foreach (string attr in match.Groups[1].Captures[0].Value.Split(SPACECOMMA, REE))
                        {
                            if (attr.StartsWith("DOCK:", OIC))
                                tags1.UnionWith(attr.Substring(5).ToUpper().Split(COLON, REE));
                        }
                    }
                    if ((match = tagRegex.Match(conn2.CustomName)).Success)
                    {
                        foreach (string attr in match.Groups[1].Captures[0].Value.Split(SPACECOMMA, REE))
                        {
                            if (attr.StartsWith("DOCK:", OIC))
                                tags2.UnionWith(attr.Substring(5).ToUpper().Split(COLON, REE));
                        }
                    }
                    if ((tags1.Count > 0 | tags2.Count > 0) & !tags1.Overlaps(tags2))
                        continue;
                    g1 = block.CubeGrid;
                    g2 = conn2.CubeGrid;
                    if (!gridShip.TryGetValue(g1, out s1))
                    {
                        gridShip[g1] = s1 = shipGrids.Count;
                        shipGrids.Add(new HashSet<IMyCubeGrid> { g1 });
                        shipName.Add(g1.CustomName);
                    }
                    if (!gridShip.TryGetValue(g2, out s2))
                    {
                        gridShip[g2] = s2 = shipGrids.Count;
                        shipGrids.Add(new HashSet<IMyCubeGrid> { g2 });
                        shipName.Add(g2.CustomName);
                    }
                    ((shipShipDocks.TryGetValue(s1, out shipDocks) ? shipDocks : (shipShipDocks[s1] = new Dictionary<int, List<string>>())).TryGetValue(s2, out docks) ? docks : (shipShipDocks[s1][s2] = new List<string>())).Add(block.CustomName);
                    ((shipShipDocks.TryGetValue(s2, out shipDocks) ? shipDocks : (shipShipDocks[s2] = new Dictionary<int, List<string>>())).TryGetValue(s1, out docks) ? docks : (shipShipDocks[s2][s1] = new List<string>())).Add(conn2.CustomName);
                }
            }

            // starting "here", traverse all docked ships
            dockedgrids.Clear();
            dockedgrids.Add(Me.CubeGrid);
            if (!gridShip.TryGetValue(Me.CubeGrid, out s1))
                return;
            ships.Add(s1);
            dockedgrids.UnionWith(shipGrids[s1]);
            squeue.Enqueue(s1);
            while (squeue.Count > 0)
            {
                s1 = squeue.Dequeue();
                if (!shipShipDocks.TryGetValue(s1, out shipDocks))
                    continue;
                foreach (int ship2 in shipDocks.Keys)
                {
                    if (ships.Add(ship2))
                    {
                        dockedgrids.UnionWith(shipGrids[ship2]);
                        squeue.Enqueue(ship2);
                        debugText.Add(shipName[ship2] + " docked to " + shipName[s1] + " at " + String.Join(", ", shipDocks[ship2]));
                    }
                }
            }
        }

        #endregion

        #region Inventory Scanning

        void ScanGroups()
        {
            List<IMyBlockGroup> groups = new List<IMyBlockGroup>();
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            System.Text.RegularExpressions.Match match;

            GridTerminalSystem.GetBlockGroups(groups);
            foreach (IMyBlockGroup group in groups)
            {
                if ((match = tagRegex.Match(group.Name)).Success)
                {
                    group.GetBlocks(blocks);
                    foreach (IMyTerminalBlock block in blocks)
                        blockGtag[block] = match;
                }
            }
        }

        void ScanBlocks<T>() where T : class
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            System.Text.RegularExpressions.Match match;
            int i, s, n;
            IMyInventory inven;
            List<MyInventoryItem> stacks = new List<MyInventoryItem>();
            string itype, isub;
            InventoryItemData data;
            long amount, total;

            GridTerminalSystem.GetBlocksOfType<T>(blocks);
            foreach (IMyTerminalBlock block in blocks)
            {
                if (!dockedgrids.Contains(block.CubeGrid))
                    continue;
                match = tagRegex.Match(block.CustomName);
                if (match.Success)
                {
                    blockGtag.Remove(block);
                    blockTag[block] = match;
                }
                else if (blockGtag.TryGetValue(block, out match))
                {
                    blockTag[block] = match;
                }

                if ((block is IMySmallMissileLauncher & !(block is IMySmallMissileLauncherReload | block.BlockDefinition.SubtypeName == "LargeMissileLauncher")) | block is IMyLargeInteriorTurret)
                {
                    // can't sort with no conveyor port
                    invenLocked.Add(block.GetInventory(0));
                }
                else if ((block is IMyFunctionalBlock) && ((block as IMyFunctionalBlock).Enabled & block.IsFunctional))
                {
                    if ((block is IMyRefinery | block is IMyReactor | block is IMyGasGenerator) & !blockTag.ContainsKey(block))
                    {
                        // don't touch input of enabled and untagged refineries, reactors or oxygen generators
                        invenLocked.Add(block.GetInventory(0));
                    }
                    else if (block is IMyAssembler && !(block as IMyAssembler).IsQueueEmpty)
                    {
                        // don't touch input of enabled and active assemblers
                        invenLocked.Add(block.GetInventory(((block as IMyAssembler).Mode == MyAssemblerMode.Disassembly) ? 1 : 0));
                    }
                }

                i = block.InventoryCount;
                while (i-- > 0)
                {
                    inven = block.GetInventory(i);
                    stacks.Clear();
                    inven.GetItems(stacks);
                    s = stacks.Count;
                    while (s-- > 0)
                    {
                        // identify the stacked item
                        itype = "" + stacks[s].Type.TypeId;
                        itype = itype.Substring(itype.LastIndexOf('_') + 1);
                        isub = stacks[s].Type.SubtypeId.ToString();

                        // new type or subtype?
                        InventoryItemData.InitItem(itype, isub, 0L, 0.0f, stacks[s].Type.SubtypeId.ToString(), null);
                        itype = itype.ToUpper();
                        isub = isub.ToUpper();

                        // update amounts
                        amount = (long)((double)stacks[s].Amount * 1e6);
                        typeAmount[itype] += amount;
                        data = typeSubData[itype][isub];
                        data.amount += amount;
                        data.avail += amount;
                        data.invenTotal.TryGetValue(inven, out total);
                        data.invenTotal[inven] = total + amount;
                        data.invenSlot.TryGetValue(inven, out n);
                        data.invenSlot[inven] = Math.Max(n, s + 1);
                    }
                }
            }
        }

        #endregion

        #endregion

        #region Quota Processing

        void AdjustAmounts()
        {
            string itype, isub;
            long amount;
            InventoryItemData data;
            List<MyInventoryItem> stacks = new List<MyInventoryItem>();

            foreach (IMyInventory inven in invenHidden)
            {
                stacks.Clear();
                inven.GetItems(stacks);
                foreach (MyInventoryItem stack in stacks)
                {
                    itype = "" + stack.Type.TypeId;
                    itype = itype.Substring(itype.LastIndexOf('_') + 1).ToUpper();
                    isub = stack.Type.SubtypeId.ToString().ToUpper();

                    amount = (long)((double)stack.Amount * 1e6);
                    typeAmount[itype] -= amount;
                    typeSubData[itype][isub].amount -= amount;
                }
            }

            foreach (IMyInventory inven in invenLocked)
            {
                stacks.Clear();
                inven.GetItems(stacks);
                foreach (MyInventoryItem stack in stacks)
                {
                    itype = "" + stack.Type.TypeId;
                    itype = itype.Substring(itype.LastIndexOf('_') + 1).ToUpper();
                    isub = stack.Type.SubtypeId.ToString().ToUpper();

                    amount = (long)((double)stack.Amount * 1e6);
                    data = typeSubData[itype][isub];
                    data.avail -= amount;
                    data.locked += amount;
                }
            }
        }

        void ProcessQuotaPanels(bool quotaStable)
        {
            bool debug = debugLogic.Contains("quotas");
            int l, x, y, wide, size, spanx, spany, height, p, priority;
            long amount, round, total;
            float ratio;
            bool force;
            string itypeCur, itype, isub;
            string[] words, empty = new string[1] { " " };
            string[][] spanLines;
            IMyTextPanel panel2;
            IMySlimBlock slim;
            Matrix matrix = new Matrix();
            StringBuilder sb = new StringBuilder();
            List<string> qtypes = new List<string>(), errors = new List<string>(), scalesubs = new List<string>();
            Dictionary<string, SortedDictionary<string, string[]>> qtypeSubCols = new Dictionary<string, SortedDictionary<string, string[]>>();
            InventoryItemData data;
            ScreenFormatter sf;

            // reset ore "quotas"
            foreach (InventoryItemData d in typeSubData["ORE"].Values)
                d.minimum = (d.amount == 0L) ? 0L : Math.Max(d.minimum, d.amount);

            foreach (IMyTextPanel panel in qpanelPriority.Keys)
            {
                wide = panel.BlockDefinition.SubtypeName.EndsWith("Wide") ? 2 : 1;
                size = panel.BlockDefinition.SubtypeName.StartsWith("Small") ? 3 : 1;
                spanx = spany = 1;
                if (panelSpan.ContainsKey(panel))
                {
                    spanx = panelSpan[panel].A;
                    spany = panelSpan[panel].B;
                }

                // (re?)assemble (spanned?) user quota text
                spanLines = new string[spanx][];
                panel.Orientation.GetMatrix(out matrix);
                sb.Clear();
                for (y = 0; y < spany; y++)
                {
                    height = 0;
                    for (x = 0; x < spanx; x++)
                    {
                        spanLines[x] = empty;
                        slim = panel.CubeGrid.GetCubeBlock(new Vector3I(panel.Position + x * wide * size * matrix.Right + y * size * matrix.Down));
                        panel2 = (slim != null) ? (slim.FatBlock as IMyTextPanel) : null;
                        if (panel2 != null && ("" + panel2.BlockDefinition == "" + panel.BlockDefinition & panel2.GetPublicTitle().ToUpper().Contains("QUOTAS")))
                        {
                            spanLines[x] = panel2.GetPublicText().Split('\n');
                            height = Math.Max(height, spanLines[x].Length);
                        }
                    }
                    for (l = 0; l < height; l++)
                    {
                        for (x = 0; x < spanx; x++)
                            sb.Append((l < spanLines[x].Length) ? spanLines[x][l] : " ");
                        sb.Append("\n");
                    }
                }

                // parse user quotas
                priority = qpanelPriority[panel];
                itypeCur = "";
                qtypes.Clear();
                qtypeSubCols.Clear();
                errors.Clear();
                foreach (string line in sb.ToString().Split('\n'))
                {
                    words = line.ToUpper().Split(SPACE, 4, REE);
                    if (words.Length >= 1)
                    {
                        if (ParseItemValueText(null, words, itypeCur, out itype, out isub, out p, out amount, out ratio, out force) & itype == itypeCur & itype != "" & isub != "")
                        {
                            data = typeSubData[itype][isub];
                            qtypeSubCols[itype][isub] = new[] { data.label, "" + Math.Round(amount / 1e6, 2), "" + Math.Round(ratio * 100.0f, 2) + "%" };
                            if ((priority > 0 & (priority < data.qpriority | data.qpriority <= 0)) | (priority == 0 & data.qpriority < 0))
                            {
                                data.qpriority = priority;
                                data.minimum = amount;
                                data.ratio = ratio;
                            }
                            else if (priority == data.qpriority)
                            {
                                data.minimum = Math.Max(data.minimum, amount);
                                data.ratio = Math.Max(data.ratio, ratio);
                            }
                        }
                        else if (ParseItemValueText(null, words, "", out itype, out isub, out p, out amount, out ratio, out force) & itype != itypeCur & itype != "" & isub == "")
                        {
                            if (!qtypeSubCols.ContainsKey(itypeCur = itype))
                            {
                                qtypes.Add(itypeCur);
                                qtypeSubCols[itypeCur] = new SortedDictionary<string, string[]>();
                            }
                        }
                        else if (itypeCur != "")
                        {
                            qtypeSubCols[itypeCur][words[0]] = words;
                        }
                        else
                        {
                            errors.Add(line);
                        }
                    }
                }

                // redraw quotas
                sf = new ScreenFormatter(4, 2);
                sf.SetAlign(1, 1);
                sf.SetAlign(2, 1);
                if (qtypes.Count == 0 & qpanelTypes[panel].Count == 0)
                    qpanelTypes[panel].AddRange(types);
                foreach (string qtype in qpanelTypes[panel])
                {
                    if (!qtypeSubCols.ContainsKey(qtype))
                    {
                        qtypes.Add(qtype);
                        qtypeSubCols[qtype] = new SortedDictionary<string, string[]>();
                    }
                }
                foreach (string qtype in qtypes)
                {
                    if (qtype == "ORE")
                        continue;
                    if (sf.GetNumRows() > 0)
                        sf.AddBlankRow();
                    sf.Add(0, typeLabel[qtype], true);
                    sf.Add(1, "  Min", true);
                    sf.Add(2, "  Pct", true);
                    sf.Add(3, "", true);
                    sf.AddBlankRow();
                    foreach (InventoryItemData d in typeSubData[qtype].Values)
                    {
                        if (!qtypeSubCols[qtype].ContainsKey(d.subType))
                            qtypeSubCols[qtype][d.subType] = new[] { d.label, "" + Math.Round(d.minimum / 1e6, 2), "" + Math.Round(d.ratio * 100.0f, 2) + "%" };
                    }
                    foreach (string qsub in qtypeSubCols[qtype].Keys)
                    {
                        words = qtypeSubCols[qtype][qsub];
                        sf.Add(0, typeSubData[qtype].ContainsKey(qsub) ? words[0] : words[0].ToLower(), true);
                        sf.Add(1, (words.Length > 1) ? words[1] : "", true);
                        sf.Add(2, (words.Length > 2) ? words[2] : "", true);
                        sf.Add(3, (words.Length > 3) ? words[3] : "", true);
                    }
                }
                WriteTableToPanel("TIM Quotas", sf, panel, true, ((errors.Count == 0) ? "" : (String.Join("\n", errors).Trim().ToLower() + "\n\n")));
            }

            // update effective quotas
            foreach (string qtype in types)
            {
                round = 1L;
                if (!FRACTIONAL_TYPES.Contains(qtype))
                    round = 1000000L;
                total = typeAmount[qtype];
                if (quotaStable & total > 0L)
                {
                    scalesubs.Clear();
                    foreach (InventoryItemData d in typeSubData[qtype].Values)
                    {
                        if (d.ratio > 0.0f & total >= (long)(d.minimum / d.ratio))
                            scalesubs.Add(d.subType);
                    }
                    if (scalesubs.Count > 0)
                    {
                        scalesubs.Sort((string s1, string s2) =>
                        {
                            InventoryItemData d1 = typeSubData[qtype][s1], d2 = typeSubData[qtype][s2];
                            long q1 = (long)(d1.amount / d1.ratio), q2 = (long)(d2.amount / d2.ratio);
                            return (q1 == q2) ? d1.ratio.CompareTo(d2.ratio) : q1.CompareTo(q2);
                        });
                        isub = scalesubs[(scalesubs.Count - 1) / 2];
                        data = typeSubData[qtype][isub];
                        total = (long)(data.amount / data.ratio + 0.5f);
                        if (debug)
                        {
                            debugText.Add("median " + typeLabel[qtype] + " is " + subLabel[isub] + ", " + (total / 1e6) + " -> " + (data.amount / 1e6 / data.ratio));
                            foreach (string qsub in scalesubs)
                            {
                                data = typeSubData[qtype][qsub];
                                debugText.Add("  " + subLabel[qsub] + " @ " + (data.amount / 1e6) + " / " + data.ratio + " => " + (long)(data.amount / 1e6 / data.ratio + 0.5f));
                            }
                        }
                    }
                }
                foreach (InventoryItemData d in typeSubData[qtype].Values)
                {
                    amount = Math.Max(d.quota, Math.Max(d.minimum, (long)(d.ratio * total + 0.5f)));
                    d.quota = (amount / round) * round;
                }
            }
        }

        #endregion

        #region Directive Parsing

        void ParseBlockTags()
        {
            StringBuilder name = new StringBuilder();
            IMyTextPanel blkPnl;
            IMyRefinery blkRfn;
            IMyAssembler blkAsm;
            System.Text.RegularExpressions.Match match;
            int i, priority, spanwide, spantall;
            string[] attrs, fields;
            string attr, itype, isub;
            long amount;
            float ratio;
            bool grouped, force, egg = false;

            // loop over all tagged blocks
            foreach (IMyTerminalBlock block in blockTag.Keys)
            {
                match = blockTag[block];
                attrs = match.Groups[1].Captures[0].Value.Split(SPACECOMMA, REE);
                name.Clear();
                if (!(grouped = blockGtag.ContainsKey(block)))
                {
                    name.Append(block.CustomName, 0, match.Index);
                    name.Append(argTagOpen);
                    if (argTagPrefix != "")
                        name.Append(argTagPrefix + " ");
                }

                // loop over all tag attributes
                if ((blkPnl = (block as IMyTextPanel)) != null)
                {
                    foreach (string a in attrs)
                    {
                        attr = a.ToUpper();
                        fields = attr.Split(COLON);
                        attr = fields[0];

                        if (attr.Length >= 4 & "STATUS".StartsWith(attr))
                        {
                            if (blkPnl.Enabled) statusPanels.Add(blkPnl);
                            name.Append("STATUS ");
                        }
                        else if (attr.Length >= 5 & "DEBUGGING".StartsWith(attr))
                        {
                            if (blkPnl.Enabled) debugPanels.Add(blkPnl);
                            name.Append("DEBUG ");
                        }
                        else if (attr == "SPAN")
                        {
                            if (fields.Length >= 3 && (int.TryParse(fields[1], out spanwide) & int.TryParse(fields[2], out spantall) & spanwide >= 1 & spantall >= 1))
                            {
                                panelSpan[blkPnl] = new Pair(spanwide, spantall);
                                name.Append("SPAN:" + spanwide + ":" + spantall + " ");
                            }
                            else
                            {
                                name.Append((attr = String.Join(":", fields).ToLower()) + " ");
                                debugText.Add("Invalid panel span rule: " + attr);
                            }
                        }
                        else if (attr == "THE")
                        {
                            egg = true;
                        }
                        else if (attr == "ENCHANTER" & egg)
                        {
                            egg = false;
                            blkPnl.SetValueFloat("FontSize", 0.2f);
                            blkPnl.WritePublicTitle("TIM the Enchanter");
                            //blkPnl.WritePublicText(panelFiller, false);
                            blkPnl.ShowPublicTextOnScreen();
                            name.Append("THE ENCHANTER ");
                        }
                        else if (attr.Length >= 3 & "QUOTAS".StartsWith(attr))
                        {
                            if (blkPnl.Enabled & !qpanelPriority.ContainsKey(blkPnl)) qpanelPriority[blkPnl] = 0;
                            if (blkPnl.Enabled & !qpanelTypes.ContainsKey(blkPnl)) qpanelTypes[blkPnl] = new List<string>();
                            name.Append("QUOTA");
                            i = 0;
                            while (++i < fields.Length)
                            {
                                if (ParseItemTypeSub(null, true, fields[i], "", out itype, out isub) & itype != "ORE" & isub == "")
                                {
                                    if (blkPnl.Enabled) qpanelTypes[blkPnl].Add(itype);
                                    name.Append(":" + typeLabel[itype]);
                                }
                                else if (fields[i].StartsWith("P") & int.TryParse(fields[i].Substring(Math.Min(1, fields[i].Length)), out priority))
                                {
                                    if (blkPnl.Enabled) qpanelPriority[blkPnl] = Math.Max(0, priority);
                                    if (priority > 0) name.Append(":P" + priority);
                                }
                                else
                                {
                                    name.Append(":" + fields[i].ToLower());
                                    debugText.Add("Invalid quota panel rule: " + fields[i].ToLower());
                                }
                            }
                            name.Append(" ");
                        }
                        else if (attr.Length >= 3 & "INVENTORY".StartsWith(attr))
                        {
                            if (blkPnl.Enabled & !ipanelTypes.ContainsKey(blkPnl)) ipanelTypes[blkPnl] = new List<string>();
                            name.Append("INVEN");
                            i = 0;
                            while (++i < fields.Length)
                            {
                                if (ParseItemTypeSub(null, true, fields[i], "", out itype, out isub) & isub == "")
                                {
                                    if (blkPnl.Enabled) ipanelTypes[blkPnl].Add(itype);
                                    name.Append(":" + typeLabel[itype]);
                                }
                                else
                                {
                                    name.Append(":" + fields[i].ToLower());
                                    debugText.Add("Invalid inventory panel rule: " + fields[i].ToLower());
                                }
                            }
                            name.Append(" ");
                        }
                        else
                        {
                            name.Append((attr = String.Join(":", fields).ToLower()) + " ");
                            debugText.Add("Invalid panel attribute: " + attr);
                        }
                    }
                }
                else
                {
                    blkRfn = (block as IMyRefinery);
                    blkAsm = (block as IMyAssembler);
                    foreach (string a in attrs)
                    {
                        attr = a.ToUpper();
                        fields = attr.Split(COLON);
                        attr = fields[0];

                        if ((attr.Length >= 4 & "LOCKED".StartsWith(attr)) | attr == "EXEMPT")
                        { // EXEMPT for AIS compat
                            i = block.InventoryCount;
                            while (i-- > 0)
                                invenLocked.Add(block.GetInventory(i));
                            name.Append(attr + " ");
                        }
                        else if (attr == "HIDDEN")
                        {
                            i = block.InventoryCount;
                            while (i-- > 0)
                                invenHidden.Add(block.GetInventory(i));
                            name.Append("HIDDEN ");
                        }
                        else if ((block is IMyShipConnector) & attr == "DOCK")
                        {
                            // handled in ScanGrids(), just rewrite
                            name.Append(String.Join(":", fields) + " ");
                        }
                        else if ((blkRfn != null | blkAsm != null) & attr == "AUTO")
                        {
                            name.Append("AUTO");
                            HashSet<string> ores, autoores = (blkRfn == null | fields.Length > 1) ? (new HashSet<string>()) : GetBlockAcceptedSubs(blkRfn, "ORE");
                            HashSet<ItemId> items, autoitems = new HashSet<ItemId>();
                            i = 0;
                            while (++i < fields.Length)
                            {
                                if (ParseItemTypeSub(null, true, fields[i], (blkRfn != null) ? "ORE" : "", out itype, out isub) & (blkRfn != null) == (itype == "ORE") & (blkRfn != null | itype != "INGOT"))
                                {
                                    if (isub == "")
                                    {
                                        if (blkRfn != null)
                                        {
                                            autoores.UnionWith(typeSubs[itype]);
                                        }
                                        else
                                        {
                                            foreach (string s in typeSubs[itype])
                                                autoitems.Add(new ItemId(itype, s));
                                        }
                                        name.Append(":" + typeLabel[itype]);
                                    }
                                    else
                                    {
                                        if (blkRfn != null)
                                        {
                                            autoores.Add(isub);
                                        }
                                        else
                                        {
                                            autoitems.Add(new ItemId(itype, isub));
                                        }
                                        name.Append(":" + ((blkRfn == null & subTypes[isub].Count > 1) ? (typeLabel[itype] + "/") : "") + subLabel[isub]);
                                    }
                                }
                                else
                                {
                                    name.Append(":" + fields[i].ToLower());
                                    debugText.Add("Unrecognized or ambiguous item: " + fields[i].ToLower());
                                }
                            }
                            if (blkRfn != null)
                            {
                                if (blkRfn.Enabled)
                                    (refineryOres.TryGetValue(blkRfn, out ores) ? ores : (refineryOres[blkRfn] = new HashSet<string>())).UnionWith(autoores);
                            }
                            else if (blkAsm.Enabled)
                                (assemblerItems.TryGetValue(blkAsm, out items) ? items : (assemblerItems[blkAsm] = new HashSet<ItemId>())).UnionWith(autoitems);
                            name.Append(" ");
                        }
                        else if (!ParseItemValueText(block, fields, "", out itype, out isub, out priority, out amount, out ratio, out force))
                        {
                            name.Append((attr = String.Join(":", fields).ToLower()) + " ");
                            debugText.Add("Unrecognized or ambiguous item: " + attr);
                        }
                        else if (!block.HasInventory | (block is IMySmallMissileLauncher & !(block is IMySmallMissileLauncherReload | block.BlockDefinition.SubtypeName == "LargeMissileLauncher")) | block is IMyLargeInteriorTurret)
                        {
                            name.Append(String.Join(":", fields).ToLower() + " ");
                            debugText.Add("Cannot sort items to " + block.CustomName + ": no conveyor-connected inventory");
                        }
                        else
                        {
                            if (isub == "")
                            {
                                foreach (string s in (force ? (IEnumerable<string>)typeSubs[itype] : (IEnumerable<string>)GetBlockAcceptedSubs(block, itype)))
                                    AddInvenRequest(block, 0, itype, s, priority, amount);
                            }
                            else
                            {
                                AddInvenRequest(block, 0, itype, isub, priority, amount);
                            }
                            if (argRewriteTags & !grouped)
                            {
                                if (force)
                                {
                                    name.Append("FORCE:" + typeLabel[itype]);
                                    if (isub != "")
                                        name.Append("/" + subLabel[isub]);
                                }
                                else if (isub == "")
                                {
                                    name.Append(typeLabel[itype]);
                                }
                                else if (subTypes[isub].Count == 1 || GetBlockImpliedType(block, isub) == itype)
                                {
                                    name.Append(subLabel[isub]);
                                }
                                else
                                {
                                    name.Append(typeLabel[itype] + "/" + subLabel[isub]);
                                }
                                if (priority > 0 & priority < int.MaxValue)
                                    name.Append(":P" + priority);
                                if (amount >= 0L)
                                    name.Append(":" + (amount / 1e6));
                                name.Append(" ");
                            }
                        }
                    }
                }

                if (argRewriteTags & !grouped)
                {
                    if (name[name.Length - 1] == ' ')
                        name.Length--;
                    name.Append(argTagClose).Append(block.CustomName, match.Index + match.Length, block.CustomName.Length - match.Index - match.Length);
                    block.CustomName = name.ToString();
                }

                if (block.GetUserRelationToOwner(Me.OwnerId) != MyRelationsBetweenPlayerAndBlock.Owner & block.GetUserRelationToOwner(Me.OwnerId) != MyRelationsBetweenPlayerAndBlock.FactionShare)
                    debugText.Add("Cannot control \"" + block.CustomName + "\" due to differing ownership");
            }
        }

        bool ParseItemTypeSub(IMyCubeBlock block, bool force, string typesub, string qtype, out string itype, out string isub)
        {
            int t, s, found;
            string[] parts;

            itype = "";
            isub = "";
            found = 0;
            parts = typesub.Trim().Split('/');
            if (parts.Length >= 2)
            {
                parts[0] = parts[0].Trim();
                parts[1] = parts[1].Trim();
                if (typeSubs.ContainsKey(parts[0]) && (parts[1] == "" | typeSubData[parts[0]].ContainsKey(parts[1])))
                {
                    // exact type/subtype
                    if (force || BlockAcceptsTypeSub(block, parts[0], parts[1]))
                    {
                        found = 1;
                        itype = parts[0];
                        isub = parts[1];
                    }
                }
                else
                {
                    // type/subtype?
                    t = types.BinarySearch(parts[0]);
                    t = Math.Max(t, ~t);
                    while ((found < 2 & t < types.Count) && types[t].StartsWith(parts[0]))
                    {
                        s = typeSubs[types[t]].BinarySearch(parts[1]);
                        s = Math.Max(s, ~s);
                        while ((found < 2 & s < typeSubs[types[t]].Count) && typeSubs[types[t]][s].StartsWith(parts[1]))
                        {
                            if (force || BlockAcceptsTypeSub(block, types[t], typeSubs[types[t]][s]))
                            {
                                found++;
                                itype = types[t];
                                isub = typeSubs[types[t]][s];
                            }
                            s++;
                        }
                        // special case for gravel
                        if (found == 0 & types[t] == "INGOT" & "GRAVEL".StartsWith(parts[1]) & (force || BlockAcceptsTypeSub(block, "INGOT", "STONE")))
                        {
                            found++;
                            itype = "INGOT";
                            isub = "STONE";
                        }
                        t++;
                    }
                }
            }
            else if (typeSubs.ContainsKey(parts[0]))
            {
                // exact type
                if (force || BlockAcceptsTypeSub(block, parts[0], ""))
                {
                    found++;
                    itype = parts[0];
                    isub = "";
                }
            }
            else if (subTypes.ContainsKey(parts[0]))
            {
                // exact subtype
                if (qtype != "" && typeSubData[qtype].ContainsKey(parts[0]))
                {
                    found++;
                    itype = qtype;
                    isub = parts[0];
                }
                else
                {
                    t = subTypes[parts[0]].Count;
                    while (found < 2 & t-- > 0)
                    {
                        if (force || BlockAcceptsTypeSub(block, subTypes[parts[0]][t], parts[0]))
                        {
                            found++;
                            itype = subTypes[parts[0]][t];
                            isub = parts[0];
                        }
                    }
                }
            }
            else if (qtype != "")
            {
                // subtype of a known type
                s = typeSubs[qtype].BinarySearch(parts[0]);
                s = Math.Max(s, ~s);
                while ((found < 2 & s < typeSubs[qtype].Count) && typeSubs[qtype][s].StartsWith(parts[0]))
                {
                    found++;
                    itype = qtype;
                    isub = typeSubs[qtype][s];
                    s++;
                }
                // special case for gravel
                if (found == 0 & qtype == "INGOT" & "GRAVEL".StartsWith(parts[0]))
                {
                    found++;
                    itype = "INGOT";
                    isub = "STONE";
                }
            }
            else
            {
                // type?
                t = types.BinarySearch(parts[0]);
                t = Math.Max(t, ~t);
                while ((found < 2 & t < types.Count) && types[t].StartsWith(parts[0]))
                {
                    if (force || BlockAcceptsTypeSub(block, types[t], ""))
                    {
                        found++;
                        itype = types[t];
                        isub = "";
                    }
                    t++;
                }
                // subtype?
                s = subs.BinarySearch(parts[0]);
                s = Math.Max(s, ~s);
                while ((found < 2 & s < subs.Count) && subs[s].StartsWith(parts[0]))
                {
                    t = subTypes[subs[s]].Count;
                    while (found < 2 & t-- > 0)
                    {
                        if (force || BlockAcceptsTypeSub(block, subTypes[subs[s]][t], subs[s]))
                        {
                            if (found != 1 || (itype != subTypes[subs[s]][t] | isub != "" | typeSubs[itype].Count != 1))
                                found++;
                            itype = subTypes[subs[s]][t];
                            isub = subs[s];
                        }
                    }
                    s++;
                }
                // special case for gravel
                if (found == 0 & "GRAVEL".StartsWith(parts[0]) & (force || BlockAcceptsTypeSub(block, "INGOT", "STONE")))
                {
                    found++;
                    itype = "INGOT";
                    isub = "STONE";
                }
            }

            // fill in implied subtype
            if (!force & block != null & found == 1 & isub == "")
            {
                HashSet<string> mysubs = GetBlockAcceptedSubs(block, itype);
                if (mysubs.Count == 1)
                    isub = mysubs.First();
            }

            return (found == 1);
        }

        bool ParseItemValueText(IMyCubeBlock block, string[] fields, string qtype, out string itype, out string isub, out int priority, out long amount, out float ratio, out bool force)
        {
            int f, l;
            double val, mul;

            itype = "";
            isub = "";
            priority = 0;
            amount = -1L;
            ratio = -1.0f;
            force = (block == null);

            // identify the item
            f = 0;
            if (fields[0].Trim() == "FORCE")
            {
                if (fields.Length == 1)
                    return false;
                force = true;
                f = 1;
            }
            if (!ParseItemTypeSub(block, force, fields[f], qtype, out itype, out isub))
                return false;

            // parse the remaining fields
            while (++f < fields.Length)
            {
                fields[f] = fields[f].Trim();
                l = fields[f].Length;

                if (l != 0)
                {
                    if (fields[f] == "IGNORE")
                    {
                        amount = 0L;
                    }
                    else if (fields[f] == "OVERRIDE" | fields[f] == "SPLIT")
                    {
                        // these AIS tags are TIM's default behavior anyway
                    }
                    else if (fields[f][l - 1] == '%' & double.TryParse(fields[f].Substring(0, l - 1), out val))
                    {
                        ratio = Math.Max(0.0f, (float)(val / 100.0));
                    }
                    else if (fields[f][0] == 'P' & double.TryParse(fields[f].Substring(1), out val))
                    {
                        priority = Math.Max(1, (int)(val + 0.5));
                    }
                    else
                    {
                        // check for numeric suffixes
                        mul = 1.0;
                        if (fields[f][l - 1] == 'K')
                        {
                            l--;
                            mul = 1e3;
                        }
                        else if (fields[f][l - 1] == 'M')
                        {
                            l--;
                            mul = 1e6;
                        }

                        // try parsing the field as an amount value
                        if (double.TryParse(fields[f].Substring(0, l), out val))
                            amount = Math.Max(0L, (long)(val * mul * 1e6 + 0.5));
                    }
                }
            }

            return true;
        }

        #endregion

        #region Item Transfer Functions

        void AddInvenRequest(IMyTerminalBlock block, int inv, string itype, string isub, int priority, long amount)
        {
            long a;
            Dictionary<string, Dictionary<string, Dictionary<IMyInventory, long>>> tsir;
            Dictionary<string, Dictionary<IMyInventory, long>> sir;
            Dictionary<IMyInventory, long> ir;

            // no priority -> last priority
            if (priority == 0)
                priority = int.MaxValue;

            // new priority/type/sub?
            tsir = (priTypeSubInvenRequest.TryGetValue(priority, out tsir) ? tsir : (priTypeSubInvenRequest[priority] = new Dictionary<string, Dictionary<string, Dictionary<IMyInventory, long>>>()));
            sir = (tsir.TryGetValue(itype, out sir) ? sir : (tsir[itype] = new Dictionary<string, Dictionary<IMyInventory, long>>()));
            ir = (sir.TryGetValue(isub, out ir) ? ir : (sir[isub] = new Dictionary<IMyInventory, long>()));

            // update request
            IMyInventory inven = block.GetInventory(inv);
            ir.TryGetValue(inven, out a);
            ir[inven] = amount;
            typeSubData[itype][isub].quota += Math.Min(0L, -a) + Math.Max(0L, amount);

            // disable conveyor for some block types
            // (IMyInventoryOwner is supposedly obsolete but there's no other way to do this for all of these block types at once)
            if (inven.Owner != null)
            {
                if (block is IMyRefinery && (block as IMyProductionBlock).UseConveyorSystem)
                {
                    block.GetActionWithName("UseConveyor").Apply(block);
                    debugText.Add("Disabling conveyor system for " + block.CustomName);
                }

                if (block is IMyGasGenerator && (block as IMyGasGenerator).UseConveyorSystem)
                {
                    block.GetActionWithName("UseConveyor").Apply(block);
                    debugText.Add("Disabling conveyor system for " + block.CustomName);
                }

                if (block is IMyReactor && (block as IMyReactor).UseConveyorSystem)
                {
                    block.GetActionWithName("UseConveyor").Apply(block);
                    debugText.Add("Disabling conveyor system for " + block.CustomName);
                }

                if (block is IMyLargeConveyorTurretBase && ((IMyLargeConveyorTurretBase)block).UseConveyorSystem)
                {
                    block.GetActionWithName("UseConveyor").Apply(block);
                    debugText.Add("Disabling conveyor system for " + block.CustomName);
                }

                if (block is IMySmallGatlingGun && ((IMySmallGatlingGun)block).UseConveyorSystem)
                {
                    block.GetActionWithName("UseConveyor").Apply(block);
                    debugText.Add("Disabling conveyor system for " + block.CustomName);
                }

                if (block is IMySmallMissileLauncher && ((IMySmallMissileLauncher)block).UseConveyorSystem)
                {
                    block.GetActionWithName("UseConveyor").Apply(block);
                    debugText.Add("Disabling conveyor system for " + block.CustomName);
                }
            }
        }

        // ================ local persisted vars ================
        List<int> AllocateItems_priorities = null;
        int AllocateItems_prioritiesIndex;
        List<string> AllocateItems_inventoryRequestTypes = null;
        int AllocateItems_inventoryRequestTypesIndex;
        List<string> AllocateItems_inventoryRequestSubTypes = null;
        int AllocateItems_inventoryRequestSubTypesIndex;
        /// <summary>
        /// Allocates all inventory items.
        /// </summary>
        /// <param name="limited">Whether to allocate limited or unlimited items.</param>
        void AllocateItems(bool limited)
        {
            // establish priority order, adding 0 for refinery management
            if (AllocateItems_priorities == null) // if not null, then ignore and continue what we were doing
            {
                AllocateItems_priorities = new List<int>(priTypeSubInvenRequest.Keys);
                AllocateItems_priorities.Sort();
                AllocateItems_prioritiesIndex = 0;
            }
            // indexes and lists are stored in a way that enables persisting between calls
            // this enables us to continue where we left of if we need to
            for (; AllocateItems_prioritiesIndex < AllocateItems_priorities.Count; AllocateItems_prioritiesIndex++)
            {
                if (AllocateItems_inventoryRequestTypes == null) // if not null, then ignore and continue what we were doing
                {
                    AllocateItems_inventoryRequestTypes = new List<string>(priTypeSubInvenRequest
                        [AllocateItems_priorities[AllocateItems_prioritiesIndex]].Keys);
                    AllocateItems_inventoryRequestTypesIndex = 0;
                }
                for (; AllocateItems_inventoryRequestTypesIndex < AllocateItems_inventoryRequestTypes.Count; AllocateItems_inventoryRequestTypesIndex++)
                {
                    if (AllocateItems_inventoryRequestSubTypes == null) // if not null, then ignore and continue what we were doing
                    {
                        AllocateItems_inventoryRequestSubTypes = new List<string>(priTypeSubInvenRequest
                            [AllocateItems_priorities[AllocateItems_prioritiesIndex]]
                                [AllocateItems_inventoryRequestTypes[AllocateItems_inventoryRequestTypesIndex]].Keys);
                        AllocateItems_inventoryRequestSubTypesIndex = 0;
                    }
                    bool doneAtLeast1Allocation = false;
                    for (; AllocateItems_inventoryRequestSubTypesIndex < AllocateItems_inventoryRequestSubTypes.Count; AllocateItems_inventoryRequestSubTypesIndex++)
                    {
                        // we check the exectution limit to ensure that we haven't gone over.
                        // if we do, then the variables for the loops should persist
                        // and since they are not set to null, we should restart the loops exactly where we stopped.
                        // this is done now to allow the index vars to increment on each iteration first (in order to not do the same
                        // thing twice).
                        if (doneAtLeast1Allocation) // only check if at least 1 allocation was completed (stops infinite loops occuring)
                            DoExecutionLimitCheck();
                        AllocateItemBatch(limited, // limited var
                            AllocateItems_priorities[AllocateItems_prioritiesIndex], // current priority
                            AllocateItems_inventoryRequestTypes[AllocateItems_inventoryRequestTypesIndex], // current type
                            AllocateItems_inventoryRequestSubTypes[AllocateItems_inventoryRequestSubTypesIndex]); // current subtype
                        doneAtLeast1Allocation = true;
                    }
                    // clear list so we know that it was completed
                    AllocateItems_inventoryRequestSubTypes = null;
                }
                // clear list so we know that it was completed
                AllocateItems_inventoryRequestTypes = null;
            }
            // clear list so we know that it was completed
            AllocateItems_priorities = null;

            // if we just finished the unlimited requests, check for leftovers
            if (!limited)
            {
                foreach (string itype in types)
                {
                    foreach (InventoryItemData data in typeSubData[itype].Values)
                    {
                        if (data.avail > 0L)
                            debugText.Add("No place to put " + GetShorthand(data.avail) + " " + typeLabel[itype] + "/" + subLabel[data.subType] + ", containers may be full");
                    }
                }
            }
        }

        void AllocateItemBatch(bool limited, int priority, string itype, string isub)
        {
            bool debug = debugLogic.Contains("sorting");
            int locked, dropped;
            long totalrequest, totalavail, request, avail, amount, moved, round;
            List<IMyInventory> invens = null;
            Dictionary<IMyInventory, long> invenRequest;

            if (debug) debugText.Add("sorting " + typeLabel[itype] + "/" + subLabel[isub] + " lim=" + limited + " p=" + priority);

            round = 1L;
            if (!FRACTIONAL_TYPES.Contains(itype))
                round = 1000000L;
            invenRequest = new Dictionary<IMyInventory, long>();
            InventoryItemData data = typeSubData[itype][isub];

            // sum up the requests
            totalrequest = 0L;
            foreach (IMyInventory reqInven in priTypeSubInvenRequest[priority][itype][isub].Keys)
            {
                request = priTypeSubInvenRequest[priority][itype][isub][reqInven];
                if (request != 0L & limited == (request >= 0L))
                {
                    if (request < 0L)
                    {
                        request = 1000000L;
                        if (reqInven.MaxVolume != VRage.MyFixedPoint.MaxValue)
                            request = (long)((double)reqInven.MaxVolume * 1e6);
                    }
                    invenRequest[reqInven] = request;
                    totalrequest += request;
                }
            }
            if (debug) debugText.Add("total req=" + (totalrequest / 1e6));
            if (totalrequest <= 0L)
                return;
            totalavail = data.avail + data.locked;
            if (debug) debugText.Add("total avail=" + (totalavail / 1e6));

            // disqualify any locked invens which already have their share
            if (totalavail > 0L)
            {
                invens = new List<IMyInventory>(data.invenTotal.Keys);
                do
                {
                    locked = 0;
                    dropped = 0;
                    foreach (IMyInventory amtInven in invens)
                    {
                        avail = data.invenTotal[amtInven];
                        if (avail > 0L & invenLocked.Contains(amtInven))
                        {
                            locked++;
                            invenRequest.TryGetValue(amtInven, out request);
                            amount = (long)((double)request / totalrequest * totalavail);
                            if (limited)
                                amount = Math.Min(amount, request);
                            amount = (amount / round) * round;

                            if (avail >= amount)
                            {
                                if (debug) debugText.Add("locked " + (amtInven.Owner == null ? "???" : (amtInven.Owner as IMyTerminalBlock).CustomName) + " gets " + (amount / 1e6) + ", has " + (avail / 1e6));
                                dropped++;
                                totalrequest -= request;
                                invenRequest[amtInven] = 0L;
                                totalavail -= avail;
                                data.locked -= avail;
                                data.invenTotal[amtInven] = 0L;
                            }
                        }
                    }
                } while (locked > dropped & dropped > 0);
            }

            // allocate the remaining available items
            foreach (IMyInventory reqInven in invenRequest.Keys)
            {
                // calculate this inven's allotment
                request = invenRequest[reqInven];
                if (request <= 0L | totalrequest <= 0L | totalavail <= 0L)
                {
                    if (limited & request > 0L) debugText.Add("Insufficient " + typeLabel[itype] + "/" + subLabel[isub] + " to satisfy " + (reqInven.Owner == null ? "???" : (reqInven.Owner as IMyTerminalBlock).CustomName));
                    continue;
                }
                amount = (long)((double)request / totalrequest * totalavail);
                if (limited)
                    amount = Math.Min(amount, request);
                amount = (amount / round) * round;
                if (debug) debugText.Add((reqInven.Owner == null ? "???" : (reqInven.Owner as IMyTerminalBlock).CustomName) + " gets " + (request / 1e6) + " / " + (totalrequest / 1e6) + " of " + (totalavail / 1e6) + " = " + (amount / 1e6));
                totalrequest -= request;

                // check how much it already has
                if (data.invenTotal.TryGetValue(reqInven, out avail))
                {
                    avail = Math.Min(avail, amount);
                    amount -= avail;
                    totalavail -= avail;
                    if (invenLocked.Contains(reqInven))
                    {
                        data.locked -= avail;
                    }
                    else
                    {
                        data.avail -= avail;
                    }
                    data.invenTotal[reqInven] -= avail;
                }

                // get the rest from other unlocked invens
                moved = 0L;
                foreach (IMyInventory amtInven in invens)
                {
                    avail = Math.Min(data.invenTotal[amtInven], amount);
                    moved = 0L;
                    if (avail > 0L & invenLocked.Contains(amtInven) == false)
                    {
                        moved = TransferItem(itype, isub, avail, amtInven, reqInven);
                        amount -= moved;
                        totalavail -= moved;
                        data.avail -= moved;
                        data.invenTotal[amtInven] -= moved;
                    }
                    // if we moved some but not all, we're probably full
                    if (amount <= 0L | (moved != 0L & moved != avail))
                        break;
                }

                if (limited & amount > 0L)
                {
                    debugText.Add("Insufficient " + typeLabel[itype] + "/" + subLabel[isub] + " to satisfy " + (reqInven.Owner == null ? "???" : (reqInven.Owner as IMyTerminalBlock).CustomName));
                    continue;
                }
            }

            if (debug) debugText.Add("" + (totalavail / 1e6) + " left over");
        }


        long TransferItem(string itype, string isub, long amount, IMyInventory fromInven, IMyInventory toInven)
        {
            bool debug = debugLogic.Contains("sorting");
            List<MyInventoryItem> stacks = new List<MyInventoryItem>();
            int s;
            VRage.MyFixedPoint remaining, moved;
            uint id;
            //	double volume;
            string stype, ssub;

            remaining = (VRage.MyFixedPoint)(amount / 1e6);
            fromInven.GetItems(stacks);
            s = Math.Min(typeSubData[itype][isub].invenSlot[fromInven], stacks.Count);
            while (remaining > 0 & s-- > 0)
            {
                stype = "" + stacks[s].Type.TypeId;
                stype = stype.Substring(stype.LastIndexOf('_') + 1).ToUpper();
                ssub = stacks[s].Type.SubtypeId.ToString().ToUpper();
                if (stype == itype & ssub == isub)
                {
                    moved = stacks[s].Amount;
                    id = stacks[s].ItemId;
                    //			volume = (double)fromInven.CurrentVolume;
                    if (fromInven == toInven)
                    {
                        remaining -= moved;
                        if (remaining < 0)
                            remaining = 0;
                    }
                    else if (fromInven.TransferItemTo(toInven, s, null, true, remaining))
                    {
                        stacks.Clear();
                        fromInven.GetItems(stacks);
                        if (s < stacks.Count && stacks[s].ItemId == id)
                            moved -= stacks[s].Amount;
                        if (moved <= 0)
                        {
                            if ((double)toInven.CurrentVolume < (double)toInven.MaxVolume / 2 & toInven.Owner != null)
                            {
                                VRage.ObjectBuilders.SerializableDefinitionId bdef = (toInven.Owner as IMyCubeBlock).BlockDefinition;
                                AddBlockRestriction(bdef.TypeIdString, bdef.SubtypeName, itype, isub);
                            }
                            s = 0;
                        }
                        else
                        {
                            numberTransfers++;
                            if (debug) debugText.Add(
                                "Transferred " + GetShorthand((long)((double)moved * 1e6)) + " " + typeLabel[itype] + "/" + subLabel[isub] +
                                " from " + (fromInven.Owner == null ? "???" : (fromInven.Owner as IMyTerminalBlock).CustomName) + " to " + (toInven.Owner == null ? "???" : (toInven.Owner as IMyTerminalBlock).CustomName)
                            );
                            //					volume -= (double)fromInven.CurrentVolume;
                            //					typeSubData[itype][isub].volume = (1000.0 * volume / (double)moved);
                        }
                        remaining -= moved;
                    }
                    else if (!fromInven.IsConnectedTo(toInven) & fromInven.Owner != null & toInven.Owner != null)
                    {
                        if (!blockErrors.ContainsKey(fromInven.Owner as IMyTerminalBlock))
                            blockErrors[fromInven.Owner as IMyTerminalBlock] = new HashSet<IMyTerminalBlock>();
                        blockErrors[fromInven.Owner as IMyTerminalBlock].Add(toInven.Owner as IMyTerminalBlock);
                        s = 0;
                    }
                }
            }

            return amount - (long)((double)remaining * 1e6 + 0.5);
        }

        #endregion

        #region Production Management

        void ScanProduction()
        {
            List<IMyTerminalBlock> blocks1 = new List<IMyTerminalBlock>(), blocks2 = new List<IMyTerminalBlock>();
            List<MyInventoryItem> stacks = new List<MyInventoryItem>();
            string itype, isub, isubIng;
            List<MyProductionItem> queue = new List<MyProductionItem>();
            ItemId item;

            producerWork.Clear();

            GridTerminalSystem.GetBlocksOfType<IMyGasGenerator>(blocks1, blk => dockedgrids.Contains(blk.CubeGrid));
            GridTerminalSystem.GetBlocksOfType<IMyRefinery>(blocks2, blk => dockedgrids.Contains(blk.CubeGrid));
            foreach (IMyFunctionalBlock blk in blocks1.Concat(blocks2))
            {
                stacks.Clear();
                blk.GetInventory(0).GetItems(stacks);
                if (stacks.Count > 0 & blk.Enabled)
                {
                    itype = "" + stacks[0].Type.TypeId;
                    itype = itype.Substring(itype.LastIndexOf('_') + 1).ToUpper();
                    isub = stacks[0].Type.SubtypeId.ToString().ToUpper();
                    if (typeSubs.ContainsKey(itype) & subTypes.ContainsKey(isub))
                        typeSubData[itype][isub].producers.Add(blk);
                    if (itype == "ORE" & (ORE_PRODUCT.TryGetValue(isub, out isubIng) ? isubIng : (isubIng = isub)) != "" & typeSubData["INGOT"].ContainsKey(isubIng))
                        typeSubData["INGOT"][isubIng].producers.Add(blk);
                    producerWork[blk] = new ProducerWork(new ItemId(itype, isub), (double)stacks[0].Amount);
                }
            }

            GridTerminalSystem.GetBlocksOfType<IMyAssembler>(blocks1, blk => dockedgrids.Contains(blk.CubeGrid));
            foreach (IMyAssembler blk in blocks1)
            {
                if (blk.Enabled & !blk.IsQueueEmpty & blk.Mode == MyAssemblerMode.Assembly)
                {
                    blk.GetQueue(queue);
                    if (blueprintItem.TryGetValue(queue[0].BlueprintId, out item))
                    {
                        if (typeSubs.ContainsKey(item.type) & subTypes.ContainsKey(item.subType))
                            typeSubData[item.type][item.subType].producers.Add(blk);
                        producerWork[blk] = new ProducerWork(item, (double)queue[0].Amount - blk.CurrentProgress);
                    }
                }
            }
        }

        void ManageRefineries()
        {
            if (!typeSubs.ContainsKey("ORE") | !typeSubs.ContainsKey("INGOT"))
                return;

            bool debug = debugLogic.Contains("refineries");
            string itype, itype2, isub, isub2, isubIngot;
            InventoryItemData data;
            int level, priority;
            List<string> ores = new List<string>();
            Dictionary<string, int> oreLevel = new Dictionary<string, int>();
            List<MyInventoryItem> stacks = new List<MyInventoryItem>();
            double speed, oldspeed;
            ProducerWork work;
            bool ready;
            List<IMyRefinery> refineries = new List<IMyRefinery>();

            if (debug) debugText.Add("Refinery management:");

            // scan inventory levels
            foreach (string isubOre in typeSubs["ORE"])
            {
                if (!ORE_PRODUCT.TryGetValue(isubOre, out isubIngot))
                    isubIngot = isubOre;
                if (isubIngot != "" & typeSubData["ORE"][isubOre].avail > 0L & typeSubData["INGOT"].TryGetValue(isubIngot, out data))
                {
                    if (data.quota > 0L)
                    {
                        level = (int)(100L * data.amount / data.quota);
                        ores.Add(isubOre);
                        oreLevel[isubOre] = level;
                        if (debug) debugText.Add("  " + subLabel[isubIngot] + " @ " + (data.amount / 1e6) + "/" + (data.quota / 1e6) + "," + ((isubOre == isubIngot) ? "" : (" Ore/" + subLabel[isubOre])) + " L=" + level + "%");
                    }
                }
            }

            // identify refineries that are ready for a new assignment
            foreach (IMyRefinery rfn in refineryOres.Keys)
            {
                itype = itype2 = isub = isub2 = "";
                stacks.Clear();
                rfn.GetInventory(0).GetItems(stacks);
                if (stacks.Count > 0)
                {
                    itype = "" + stacks[0].Type.TypeId;
                    itype = itype.Substring(itype.LastIndexOf('_') + 1).ToUpper();
                    isub = stacks[0].Type.SubtypeId.ToString().ToUpper();
                    if (itype == "ORE" & oreLevel.ContainsKey(isub))
                        oreLevel[isub] += Math.Max(1, oreLevel[isub] / refineryOres.Count);
                    if (stacks.Count > 1)
                    {
                        itype2 = "" + stacks[1].Type.TypeId;
                        itype2 = itype2.Substring(itype2.LastIndexOf('_') + 1).ToUpper();
                        isub2 = stacks[1].Type.SubtypeId.ToString().ToUpper();
                        if (itype2 == "ORE" & oreLevel.ContainsKey(isub2))
                            oreLevel[isub2] += Math.Max(1, oreLevel[isub2] / refineryOres.Count);
                        AddInvenRequest(rfn, 0, itype2, isub2, -2, (long)((double)stacks[1].Amount * 1e6 + 0.5));
                    }
                }
                if (producerWork.TryGetValue(rfn, out work))
                {
                    data = typeSubData[work.item.type][work.item.subType];
                    oldspeed = (data.prdSpeed.TryGetValue("" + rfn.BlockDefinition, out oldspeed) ? oldspeed : 1.0);
                    speed = ((work.item.subType == isub) ? Math.Max(work.quantity - (double)stacks[0].Amount, 0.0) : Math.Max(work.quantity, oldspeed));
                    speed = Math.Min(Math.Max((speed + oldspeed) / 2.0, 0.2), 10000.0);
                    data.prdSpeed["" + rfn.BlockDefinition] = speed;
                    if (debug & (int)(oldspeed + 0.5) != (int)(speed + 0.5)) debugText.Add("  Update " + rfn.BlockDefinition.SubtypeName + ":" + subLabel[work.item.subType] + " refine speed: " + ((int)(oldspeed + 0.5)) + " -> " + ((int)(speed + 0.5)) + "kg/cycle");
                }
                if (refineryOres[rfn].Count > 0) refineryOres[rfn].IntersectWith(oreLevel.Keys); else refineryOres[rfn].UnionWith(oreLevel.Keys);
                ready = (refineryOres[rfn].Count > 0);
                if (stacks.Count > 0)
                {
                    speed = (itype == "ORE" ? (typeSubData["ORE"][isub].prdSpeed.TryGetValue("" + rfn.BlockDefinition, out speed) ? speed : 1.0) : 1e6);
                    AddInvenRequest(rfn, 0, itype, isub, -1, (long)Math.Min((double)stacks[0].Amount * 1e6 + 0.5, 10 * speed * 1e6 + 0.5));
                    ready = (ready & itype == "ORE" & (double)stacks[0].Amount < 2.5 * speed & stacks.Count == 1);
                }
                if (ready)
                    refineries.Add(rfn);
                if (debug) debugText.Add(
                    "  " + rfn.CustomName + ((stacks.Count < 1) ? " idle" : (
                        " refining " + (int)stacks[0].Amount + "kg " + ((isub == "") ? "unknown" : (
                            subLabel[isub] + (!oreLevel.ContainsKey(isub) ? "" : (" (L=" + oreLevel[isub] + "%)"))
                        )) + ((stacks.Count < 2) ? "" : (
                            ", then " + (int)stacks[1].Amount + "kg " + ((isub2 == "") ? "unknown" : (
                                subLabel[isub2] + (!oreLevel.ContainsKey(isub2) ? "" : (" (L=" + oreLevel[isub2] + "%)"))
                            ))
                        ))
                    )) + "; " + ((oreLevel.Count == 0) ? "nothing to do" : (ready ? "ready" : ((refineryOres[rfn].Count == 0) ? "restricted" : "busy")))
                );
            }

            // skip refinery:ore assignment if there are no ores or ready refineries
            if (ores.Count > 0 & refineries.Count > 0)
            {
                ores.Sort((string o1, string o2) =>
                {
                    string i1, i2;
                    if (!ORE_PRODUCT.TryGetValue(o1, out i1)) i1 = o1;
                    if (!ORE_PRODUCT.TryGetValue(o2, out i2)) i2 = o2;
                    return -1 * typeSubData["INGOT"][i1].quota.CompareTo(typeSubData["INGOT"][i2].quota);
                });
                refineries.Sort((IMyRefinery r1, IMyRefinery r2) => refineryOres[r1].Count.CompareTo(refineryOres[r2].Count));
                foreach (IMyRefinery rfn in refineries)
                {
                    isub = "";
                    level = int.MaxValue;
                    foreach (string isubOre in ores)
                    {
                        if ((isub == "" | oreLevel[isubOre] < level) & refineryOres[rfn].Contains(isubOre))
                        {
                            isub = isubOre;
                            level = oreLevel[isub];
                        }
                    }
                    if (isub != "")
                    {
                        numberRefineres++;
                        rfn.UseConveyorSystem = false;
                        priority = rfn.GetInventory(0).IsItemAt(0) ? -4 : -3;
                        speed = (typeSubData["ORE"][isub].prdSpeed.TryGetValue("" + rfn.BlockDefinition, out speed) ? speed : 1.0);
                        AddInvenRequest(rfn, 0, "ORE", isub, priority, (long)(10 * speed * 1e6 + 0.5));
                        oreLevel[isub] += Math.Min(Math.Max((int)(oreLevel[isub] * 0.41), 1), (100 / refineryOres.Count));
                        if (debug) debugText.Add("  " + rfn.CustomName + " assigned " + ((int)(10 * speed + 0.5)) + "kg " + subLabel[isub] + " (L=" + oreLevel[isub] + "%)");
                    }
                    else if (debug) debugText.Add("  " + rfn.CustomName + " unassigned, nothing to do");
                }
            }

            for (priority = -1; priority >= -4; priority--)
            {
                if (priTypeSubInvenRequest.ContainsKey(priority))
                {
                    foreach (string isubOre in priTypeSubInvenRequest[priority]["ORE"].Keys)
                        AllocateItemBatch(true, priority, "ORE", isubOre);
                }
            }
        }

        void ManageAssemblers()
        {
            if (!typeSubs.ContainsKey("INGOT"))
                return;

            bool debug = debugLogic.Contains("assemblers");
            long ttlCmp;
            int level, amount;
            InventoryItemData data, data2;
            ItemId item, item2;
            List<ItemId> items;
            Dictionary<ItemId, int> itemLevel = new Dictionary<ItemId, int>(), itemPar = new Dictionary<ItemId, int>();
            List<MyProductionItem> queue = new List<MyProductionItem>();
            double speed, oldspeed;
            ProducerWork work;
            bool ready, jam;
            List<IMyAssembler> assemblers = new List<IMyAssembler>();

            if (debug) debugText.Add("Assembler management:");

            // scan inventory levels
            typeAmount.TryGetValue("COMPONENT", out ttlCmp);
            amount = 90 + (int)(10 * typeSubData["INGOT"].Values.Min(d => (d.subType != "URANIUM" & (d.minimum > 0L | d.ratio > 0.0f)) ? (d.amount / Math.Max(d.minimum, 17.5 * d.ratio * ttlCmp)) : 2.0));
            if (debug) debugText.Add("  Component par L=" + amount + "%");
            foreach (string itype in types)
            {
                if (itype != "ORE" & itype != "INGOT")
                {
                    foreach (string isub in typeSubs[itype])
                    {
                        data = typeSubData[itype][isub];
                        data.hold = Math.Max(0, data.hold - 1);
                        item = new ItemId(itype, isub);
                        itemPar[item] = ((itype == "COMPONENT" & data.ratio > 0.0f) ? amount : 100);
                        level = (int)(100L * data.amount / Math.Max(1L, data.quota));
                        if (data.quota > 0L & level < itemPar[item] & data.blueprint != default(MyDefinitionId))
                        {
                            if (data.hold == 0) itemLevel[item] = level;
                            if (debug) debugText.Add("  " + typeLabel[itype] + "/" + subLabel[isub] + ((data.hold > 0) ? "" : (" @ " + (data.amount / 1e6) + "/" + (data.quota / 1e6) + ", L=" + level + "%")) + ((data.hold > 0 | data.jam > 0) ? ("; HOLD " + data.hold + "/" + (10 * data.jam)) : ""));
                        }
                    }
                }
            }

            // identify assemblers that are ready for a new assignment
            foreach (IMyAssembler asm in assemblerItems.Keys)
            {
                ready = jam = false;
                data = data2 = null;
                item = item2 = new ItemId("", "");
                if (!asm.IsQueueEmpty)
                {
                    asm.GetQueue(queue);
                    data = (blueprintItem.TryGetValue(queue[0].BlueprintId, out item) ? typeSubData[item.type][item.subType] : null);
                    if (data != null & itemLevel.ContainsKey(item))
                        itemLevel[item] += Math.Max(1, (int)(1e8 * (double)queue[0].Amount / data.quota + 0.5));
                    if (queue.Count > 1 && (blueprintItem.TryGetValue(queue[1].BlueprintId, out item2) & itemLevel.ContainsKey(item2)))
                        itemLevel[item2] += Math.Max(1, (int)(1e8 * (double)queue[1].Amount / typeSubData[item2.type][item2.subType].quota + 0.5));
                }
                if (producerWork.TryGetValue(asm, out work))
                {
                    data2 = typeSubData[work.item.type][work.item.subType];
                    oldspeed = (data2.prdSpeed.TryGetValue("" + asm.BlockDefinition, out oldspeed) ? oldspeed : 1.0);
                    if (work.item.type != item.type | work.item.subType != item.subType)
                    {
                        speed = Math.Max(oldspeed, (asm.IsQueueEmpty ? 2 : 1) * work.quantity);
                        producerJam.Remove(asm);
                    }
                    else if (asm.IsProducing)
                    {
                        speed = work.quantity - (double)queue[0].Amount + asm.CurrentProgress;
                        producerJam.Remove(asm);
                    }
                    else
                    {
                        speed = Math.Max(oldspeed, work.quantity - (double)queue[0].Amount + asm.CurrentProgress);
                        if ((producerJam[asm] = (producerJam.TryGetValue(asm, out level) ? level : 0) + 1) >= 3)
                        {
                            debugText.Add("  " + asm.CustomName + " is jammed by " + subLabel[item.subType]);
                            producerJam.Remove(asm);
                            asm.ClearQueue();
                            data2.hold = 10 * ((data2.jam < 1 | data2.hold < 1) ? (data2.jam = Math.Min(10, data2.jam + 1)) : data2.jam);
                            jam = true;
                        }
                    }
                    speed = Math.Min(Math.Max((speed + oldspeed) / 2.0, Math.Max(0.2, 0.5 * oldspeed)), Math.Min(1000.0, 2.0 * oldspeed));
                    data2.prdSpeed["" + asm.BlockDefinition] = speed;
                    if (debug & (int)(oldspeed + 0.5) != (int)(speed + 0.5)) debugText.Add("  Update " + asm.BlockDefinition.SubtypeName + ":" + typeLabel[work.item.type] + "/" + subLabel[work.item.subType] + " assemble speed: " + ((int)(oldspeed * 100) / 100.0) + " -> " + ((int)(speed * 100) / 100.0) + "/cycle");
                }
                if (assemblerItems[asm].Count == 0) assemblerItems[asm].UnionWith(itemLevel.Keys); else assemblerItems[asm].IntersectWith(itemLevel.Keys);
                speed = ((data != null && data.prdSpeed.TryGetValue("" + asm.BlockDefinition, out speed)) ? speed : 1.0);
                if (!jam & (asm.IsQueueEmpty || (((double)queue[0].Amount - asm.CurrentProgress) < 2.5 * speed & queue.Count == 1 & asm.Mode == MyAssemblerMode.Assembly)))
                {
                    if (data2 != null) data2.jam = Math.Max(0, data2.jam - ((data2.hold < 1) ? 1 : 0));
                    if (ready = (assemblerItems[asm].Count > 0)) assemblers.Add(asm);
                }
                if (debug) debugText.Add(
                    "  " + asm.CustomName + (asm.IsQueueEmpty ? " idle" : (
                        ((asm.Mode == MyAssemblerMode.Assembly) ? " making " : " breaking ") + queue[0].Amount + "x " + ((item.type == "") ? "unknown" : (
                            subLabel[item.subType] + (!itemLevel.ContainsKey(item) ? "" : (" (L=" + itemLevel[item] + "%)"))
                        )) + ((queue.Count <= 1) ? "" : (
                            ", then " + queue[1].Amount + "x " + ((item2.type == "") ? "unknown" : (
                                subLabel[item2.subType] + (!itemLevel.ContainsKey(item2) ? "" : (" (L=" + itemLevel[item2] + "%)"))
                            ))
                        ))
                    )) + "; " + ((itemLevel.Count == 0) ? "nothing to do" : (ready ? "ready" : ((assemblerItems[asm].Count == 0) ? "restricted" : "busy")))
                );
            }

            // skip assembler:item assignments if there are no needed items or ready assemblers
            if (itemLevel.Count > 0 & assemblers.Count > 0)
            {
                items = new List<ItemId>(itemLevel.Keys);
                items.Sort((i1, i2) => -1 * typeSubData[i1.type][i1.subType].quota.CompareTo(typeSubData[i2.type][i2.subType].quota));
                assemblers.Sort((IMyAssembler a1, IMyAssembler a2) => assemblerItems[a1].Count.CompareTo(assemblerItems[a2].Count));
                foreach (IMyAssembler asm in assemblers)
                {
                    item = new ItemId("", "");
                    level = int.MaxValue;
                    foreach (ItemId i in items)
                    {
                        if (itemLevel[i] < Math.Min(level, itemPar[i]) & assemblerItems[asm].Contains(i) & typeSubData[i.type][i.subType].hold < 1)
                        {
                            item = i;
                            level = itemLevel[i];
                        }
                    }
                    if (item.type != "")
                    {
                        numberAssemblers++;
                        asm.UseConveyorSystem = true;
                        asm.CooperativeMode = false;
                        asm.Repeating = false;
                        asm.Mode = MyAssemblerMode.Assembly;
                        data = typeSubData[item.type][item.subType];
                        speed = (data.prdSpeed.TryGetValue("" + asm.BlockDefinition, out speed) ? speed : 1.0);
                        amount = Math.Max((int)(10 * speed), 10);
                        asm.AddQueueItem(data.blueprint, (double)amount);
                        itemLevel[item] += (int)Math.Ceiling(1e8 * amount / data.quota);
                        if (debug) debugText.Add("  " + asm.CustomName + " assigned " + amount + "x " + subLabel[item.subType] + " (L=" + itemLevel[item] + "%)");
                    }
                    else if (debug) debugText.Add("  " + asm.CustomName + " unassigned, nothing to do");
                }
            }
        }

        #endregion

        #region Panel Handling

        void UpdateInventoryPanels()
        {
            string text, header2, header5;
            Dictionary<string, List<IMyTextPanel>> itypesPanels = new Dictionary<string, List<IMyTextPanel>>();
            ScreenFormatter sf;
            long maxamt, maxqta;

            foreach (IMyTextPanel panel in ipanelTypes.Keys)
            {
                text = String.Join("/", ipanelTypes[panel]);
                if (itypesPanels.ContainsKey(text)) itypesPanels[text].Add(panel); else itypesPanels[text] = new List<IMyTextPanel>() { panel };
            }
            foreach (List<IMyTextPanel> panels in itypesPanels.Values)
            {
                sf = new ScreenFormatter(6);
                sf.SetBar(0);
                sf.SetFill(0);
                sf.SetAlign(2, 1);
                sf.SetAlign(3, 1);
                sf.SetAlign(4, 1);
                sf.SetAlign(5, 1);
                maxamt = maxqta = 0L;
                foreach (string itype in ((ipanelTypes[panels[0]].Count > 0) ? ipanelTypes[panels[0]] : types))
                {
                    header2 = " Asm ";
                    header5 = "Quota";
                    if (itype == "INGOT")
                    {
                        header2 = " Ref ";
                    }
                    else if (itype == "ORE")
                    {
                        header2 = " Ref ";
                        header5 = "Max";
                    }
                    if (sf.GetNumRows() > 0)
                        sf.AddBlankRow();
                    sf.Add(0, "");
                    sf.Add(1, typeLabel[itype], true);
                    sf.Add(2, header2, true);
                    sf.Add(3, "Qty", true);
                    sf.Add(4, " / ", true);
                    sf.Add(5, header5, true);
                    sf.AddBlankRow();
                    foreach (InventoryItemData data in typeSubData[itype].Values)
                    {
                        sf.Add(0, (data.amount == 0L) ? "0.0" : ("" + ((double)data.amount / data.quota)));
                        sf.Add(1, data.label, true);
                        text = ((data.producers.Count > 0) ? (data.producers.Count + " " + (data.producers.All(blk => (!(blk is IMyProductionBlock) || (blk as IMyProductionBlock).IsProducing)) ? " " : "!")) : ((data.hold > 0) ? "-  " : ""));
                        sf.Add(2, text, true);
                        sf.Add(3, (data.amount > 0L | data.quota > 0L) ? GetShorthand(data.amount) : "");
                        sf.Add(4, (data.quota > 0L) ? " / " : "", true);
                        sf.Add(5, (data.quota > 0L) ? GetShorthand(data.quota) : "");
                        maxamt = Math.Max(maxamt, data.amount);
                        maxqta = Math.Max(maxqta, data.quota);
                    }
                }
                sf.SetWidth(3, ScreenFormatter.GetWidth("8.88" + ((maxamt >= 1000000000000L) ? " M" : ((maxamt >= 1000000000L) ? " K" : "")), true));
                sf.SetWidth(5, ScreenFormatter.GetWidth("8.88" + ((maxqta >= 1000000000000L) ? " M" : ((maxqta >= 1000000000L) ? " K" : "")), true));
                foreach (IMyTextPanel panel in panels)
                    WriteTableToPanel("TIM Inventory", sf, panel);
            }
        }


        void UpdateStatusPanels()
        {
            long r;
            StringBuilder sb;

            if (statusPanels.Count > 0)
            {
                sb = new StringBuilder();
                sb.Append(panelStatsHeader);
                for (r = Math.Max(1, totalCallCount - statsLog.Length + 1); r <= totalCallCount; r++)
                    sb.Append(statsLog[r % statsLog.Length]);

                foreach (IMyTextPanel panel in statusPanels)
                {
                    panel.WritePublicTitle("Script Status");
                    if (panelSpan.ContainsKey(panel))
                        debugText.Add("Status panels cannot be spanned");
                    panel.WritePublicText(sb.ToString());
                    panel.ShowPublicTextOnScreen();
                }
            }

            if (debugPanels.Count > 0)
            {
                foreach (IMyTerminalBlock blockFrom in blockErrors.Keys)
                {
                    foreach (IMyTerminalBlock blockTo in blockErrors[blockFrom])
                        debugText.Add("No conveyor connection from " + blockFrom.CustomName + " to " + blockTo.CustomName);
                }
                foreach (IMyTextPanel panel in debugPanels)
                {
                    panel.WritePublicTitle("Script Debugging");
                    if (panelSpan.ContainsKey(panel))
                        debugText.Add("Debug panels cannot be spanned");
                    panel.WritePublicText(String.Join("\n", debugText));
                    panel.ShowPublicTextOnScreen();
                }
            }
            blockErrors.Clear();
        }


        void WriteTableToPanel(string title, ScreenFormatter sf, IMyTextPanel panel, bool allowspan = true, string before = "", string after = "")
        {
            int spanx, spany, rows, wide, size, width, height;
            int x, y, r;
            float fontsize;
            string[][] spanLines;
            string text;
            Matrix matrix;
            IMySlimBlock slim;
            IMyTextPanel spanpanel;

            // get the spanning dimensions, if any
            wide = panel.BlockDefinition.SubtypeName.EndsWith("Wide") ? 2 : 1;
            size = panel.BlockDefinition.SubtypeName.StartsWith("Small") ? 3 : 1;
            spanx = spany = 1;
            if (allowspan & panelSpan.ContainsKey(panel))
            {
                spanx = panelSpan[panel].A;
                spany = panelSpan[panel].B;
            }

            // reduce font size to fit everything
            x = sf.GetMinWidth();
            x = (x / spanx) + ((x % spanx > 0) ? 1 : 0);
            y = sf.GetNumRows();
            y = (y / spany) + ((y % spany > 0) ? 1 : 0);
            width = 658 * wide; // TODO monospace 26x17.5 chars
            fontsize = panel.GetValueFloat("FontSize");
            if (fontsize < 0.25f)
                fontsize = 1.0f;
            if (x > 0)
                fontsize = Math.Min(fontsize, Math.Max(0.5f, width * 100 / x / 100.0f));
            if (y > 0)
                fontsize = Math.Min(fontsize, Math.Max(0.5f, 1760 / y / 100.0f));

            // calculate how much space is available on each panel
            width = (int)(width / fontsize);
            height = (int)(17.6f / fontsize);

            // write to each panel
            if (spanx > 1 | spany > 1)
            {
                spanLines = sf.ToSpan(width, spanx);
                matrix = new Matrix();
                panel.Orientation.GetMatrix(out matrix);
                for (x = 0; x < spanx; x++)
                {
                    r = 0;
                    for (y = 0; y < spany; y++)
                    {
                        slim = panel.CubeGrid.GetCubeBlock(new Vector3I(panel.Position + x * wide * size * matrix.Right + y * size * matrix.Down));
                        if (slim != null && (slim.FatBlock is IMyTextPanel) && "" + slim.FatBlock.BlockDefinition == "" + panel.BlockDefinition)
                        {
                            spanpanel = slim.FatBlock as IMyTextPanel;
                            rows = Math.Max(0, spanLines[x].Length - r);
                            if (y + 1 < spany)
                                rows = Math.Min(rows, height);
                            text = "";
                            if (r < spanLines[x].Length)
                                text = String.Join("\n", spanLines[x], r, rows);
                            if (x == 0)
                                text += ((y == 0) ? before : (((y + 1) == spany) ? after : ""));
                            spanpanel.SetValueFloat("FontSize", fontsize);
                            spanpanel.WritePublicTitle(title + " (" + (x + 1) + "," + (y + 1) + ")");
                            spanpanel.WritePublicText(text);
                            spanpanel.ShowPublicTextOnScreen();
                        }
                        r += height;
                    }
                }
            }
            else
            {
                panel.SetValueFloat("FontSize", fontsize);
                panel.WritePublicTitle(title);
                panel.WritePublicText(before + sf.ToString(width) + after);
                panel.ShowPublicTextOnScreen();
            }
        }

        #endregion

        #region Processing Classes

        public class ScreenFormatter
        {
            private static Dictionary<char, byte> charWidth = new Dictionary<char, byte>();
            private static Dictionary<string, int> textWidth = new Dictionary<string, int>();
            private static byte SZ_SPACE;
            private static byte SZ_SHYPH;

            public static int GetWidth(string text, bool memoize = false)
            {
                int width;
                if (!textWidth.TryGetValue(text, out width))
                {
                    // this isn't faster (probably slower) but it's less "complex"
                    // according to SE's silly branch count metric
                    Dictionary<char, byte> cW = charWidth;
                    string t = text + "\0\0\0\0\0\0\0";
                    int i = t.Length - (t.Length % 8);
                    byte w0, w1, w2, w3, w4, w5, w6, w7;
                    while (i > 0)
                    {
                        cW.TryGetValue(t[i - 1], out w0);
                        cW.TryGetValue(t[i - 2], out w1);
                        cW.TryGetValue(t[i - 3], out w2);
                        cW.TryGetValue(t[i - 4], out w3);
                        cW.TryGetValue(t[i - 5], out w4);
                        cW.TryGetValue(t[i - 6], out w5);
                        cW.TryGetValue(t[i - 7], out w6);
                        cW.TryGetValue(t[i - 8], out w7);
                        width += w0 + w1 + w2 + w3 + w4 + w5 + w6 + w7;
                        i -= 8;
                    }
                    if (memoize)
                        textWidth[text] = width;
                }
                return width;
            } // GetWidth()

            public static string Format(string text, int width, out int unused, int align = -1, bool memoize = false)
            {
                int spaces, bars;

                // '\u00AD' is a "soft hyphen" in UTF16 but Panels don't wrap lines so
                // it's just a wider space character ' ', useful for column alignment
                unused = width - GetWidth(text, memoize);
                if (unused <= SZ_SPACE / 2)
                    return text;
                spaces = unused / SZ_SPACE;
                bars = 0;
                unused -= spaces * SZ_SPACE;
                if (2 * unused <= SZ_SPACE + (spaces * (SZ_SHYPH - SZ_SPACE)))
                {
                    bars = Math.Min(spaces, (int)((float)unused / (SZ_SHYPH - SZ_SPACE) + 0.4999f));
                    spaces -= bars;
                    unused -= bars * (SZ_SHYPH - SZ_SPACE);
                }
                else if (unused > SZ_SPACE / 2)
                {
                    spaces++;
                    unused -= SZ_SPACE;
                }
                if (align > 0)
                    return new String(' ', spaces) + new String('\u00AD', bars) + text;
                if (align < 0)
                    return text + new String('\u00AD', bars) + new String(' ', spaces);
                if ((spaces % 2) > 0 & (bars % 2) == 0)
                    return new String(' ', spaces / 2) + new String('\u00AD', bars / 2) + text + new String('\u00AD', bars / 2) + new String(' ', spaces - (spaces / 2));
                return new String(' ', spaces - (spaces / 2)) + new String('\u00AD', bars / 2) + text + new String('\u00AD', bars - (bars / 2)) + new String(' ', spaces / 2);
            } // Format()

            public static string Format(double value, int width, out int unused)
            {
                int spaces, bars;
                value = Math.Min(Math.Max(value, 0.0f), 1.0f);
                spaces = width / SZ_SPACE;
                bars = (int)(spaces * value + 0.5f);
                unused = width - (spaces * SZ_SPACE);
                return new String('I', bars) + new String(' ', spaces - bars);
            } // Format()

            public static void Init()
            {
                InitChars(0, "\u2028\u2029\u202F");
                InitChars(7, "'|\u00A6\u02C9\u2018\u2019\u201A");
                InitChars(8, "\u0458");
                InitChars(9, " !I`ijl\u00A0\u00A1\u00A8\u00AF\u00B4\u00B8\u00CC\u00CD\u00CE\u00CF\u00EC\u00ED\u00EE\u00EF\u0128\u0129\u012A\u012B\u012E\u012F\u0130\u0131\u0135\u013A\u013C\u013E\u0142\u02C6\u02C7\u02D8\u02D9\u02DA\u02DB\u02DC\u02DD\u0406\u0407\u0456\u0457\u2039\u203A\u2219");
                InitChars(10, "(),.1:;[]ft{}\u00B7\u0163\u0165\u0167\u021B");
                InitChars(11, "\"-r\u00AA\u00AD\u00BA\u0140\u0155\u0157\u0159");
                InitChars(12, "*\u00B2\u00B3\u00B9");
                InitChars(13, "\\\u00B0\u201C\u201D\u201E");
                InitChars(14, "\u0491");
                InitChars(15, "/\u0133\u0442\u044D\u0454");
                InitChars(16, "L_vx\u00AB\u00BB\u0139\u013B\u013D\u013F\u0141\u0413\u0433\u0437\u043B\u0445\u0447\u0490\u2013\u2022");
                InitChars(17, "7?Jcz\u00A2\u00BF\u00E7\u0107\u0109\u010B\u010D\u0134\u017A\u017C\u017E\u0403\u0408\u0427\u0430\u0432\u0438\u0439\u043D\u043E\u043F\u0441\u044A\u044C\u0453\u0455\u045C");
                InitChars(18, "3FKTabdeghknopqsuy\u00A3\u00B5\u00DD\u00E0\u00E1\u00E2\u00E3\u00E4\u00E5\u00E8\u00E9\u00EA\u00EB\u00F0\u00F1\u00F2\u00F3\u00F4\u00F5\u00F6\u00F8\u00F9\u00FA\u00FB\u00FC\u00FD\u00FE\u00FF\u00FF\u0101\u0103\u0105\u010F\u0111\u0113\u0115\u0117\u0119\u011B\u011D\u011F\u0121\u0123\u0125\u0127\u0136\u0137\u0144\u0146\u0148\u0149\u014D\u014F\u0151\u015B\u015D\u015F\u0161\u0162\u0164\u0166\u0169\u016B\u016D\u016F\u0171\u0173\u0176\u0177\u0178\u0219\u021A\u040E\u0417\u041A\u041B\u0431\u0434\u0435\u043A\u0440\u0443\u0446\u044F\u0451\u0452\u045B\u045E\u045F");
                InitChars(19, "+<=>E^~\u00AC\u00B1\u00B6\u00C8\u00C9\u00CA\u00CB\u00D7\u00F7\u0112\u0114\u0116\u0118\u011A\u0404\u040F\u0415\u041D\u042D\u2212");
                InitChars(20, "#0245689CXZ\u00A4\u00A5\u00C7\u00DF\u0106\u0108\u010A\u010C\u0179\u017B\u017D\u0192\u0401\u040C\u0410\u0411\u0412\u0414\u0418\u0419\u041F\u0420\u0421\u0422\u0423\u0425\u042C\u20AC");
                InitChars(21, "$&GHPUVY\u00A7\u00D9\u00DA\u00DB\u00DC\u00DE\u0100\u011C\u011E\u0120\u0122\u0124\u0126\u0168\u016A\u016C\u016E\u0170\u0172\u041E\u0424\u0426\u042A\u042F\u0436\u044B\u2020\u2021");
                InitChars(22, "ABDNOQRS\u00C0\u00C1\u00C2\u00C3\u00C4\u00C5\u00D0\u00D1\u00D2\u00D3\u00D4\u00D5\u00D6\u00D8\u0102\u0104\u010E\u0110\u0143\u0145\u0147\u014C\u014E\u0150\u0154\u0156\u0158\u015A\u015C\u015E\u0160\u0218\u0405\u040A\u0416\u0444");
                InitChars(23, "\u0459");
                InitChars(24, "\u044E");
                InitChars(25, "%\u0132\u042B");
                InitChars(26, "@\u00A9\u00AE\u043C\u0448\u045A");
                InitChars(27, "M\u041C\u0428");
                InitChars(28, "mw\u00BC\u0175\u042E\u0449");
                InitChars(29, "\u00BE\u00E6\u0153\u0409");
                InitChars(30, "\u00BD\u0429");
                InitChars(31, "\u2122");
                InitChars(32, "W\u00C6\u0152\u0174\u2014\u2026\u2030");
                SZ_SPACE = charWidth[' '];
                SZ_SHYPH = charWidth['\u00AD'];
            } // Init()

            private static void InitChars(byte width, string text)
            {
                // more silly loop-unrolling, as in GetWidth()
                Dictionary<char, byte> cW = charWidth;
                string t = text + "\0\0\0\0\0\0\0";
                byte w = Math.Max((byte)0, width);
                int i = t.Length - (t.Length % 8);
                while (i > 0)
                {
                    cW[t[--i]] = w;
                    cW[t[--i]] = w;
                    cW[t[--i]] = w;
                    cW[t[--i]] = w;
                    cW[t[--i]] = w;
                    cW[t[--i]] = w;
                    cW[t[--i]] = w;
                    cW[t[--i]] = w;
                }
                cW['\0'] = 0;
            } // InitChars()

            private int numCols;
            private int numRows;
            private int padding;
            private List<string>[] colRowText;
            private List<int>[] colRowWidth;
            private int[] colAlign;
            private int[] colFill;
            private bool[] colBar;
            private int[] colWidth;

            public ScreenFormatter(int numCols, int padding = 1)
            {
                this.numCols = numCols;
                numRows = 0;
                this.padding = padding;
                colRowText = new List<string>[numCols];
                colRowWidth = new List<int>[numCols];
                colAlign = new int[numCols];
                colFill = new int[numCols];
                colBar = new bool[numCols];
                colWidth = new int[numCols];
                for (int c = 0; c < numCols; c++)
                {
                    colRowText[c] = new List<string>();
                    colRowWidth[c] = new List<int>();
                    colAlign[c] = -1;
                    colFill[c] = 0;
                    colBar[c] = false;
                    colWidth[c] = 0;
                }
            } // ScreenFormatter()

            public void Add(int col, string text, bool memoize = false)
            {
                int width = 0;
                colRowText[col].Add(text);
                if (colBar[col] == false)
                {
                    width = GetWidth(text, memoize);
                    colWidth[col] = Math.Max(colWidth[col], width);
                }
                colRowWidth[col].Add(width);
                numRows = Math.Max(numRows, colRowText[col].Count);
            } // Add()

            public void AddBlankRow()
            {
                for (int c = 0; c < numCols; c++)
                {
                    colRowText[c].Add("");
                    colRowWidth[c].Add(0);
                }
                numRows++;
            } // AddBlankRow()

            public int GetNumRows()
            {
                return numRows;
            } // GetNumRows()

            public int GetMinWidth()
            {
                int width = padding * SZ_SPACE;
                for (int c = 0; c < numCols; c++)
                    width += padding * SZ_SPACE + colWidth[c];
                return width;
            } // GetMinWidth()

            public void SetAlign(int col, int align)
            {
                colAlign[col] = align;
            } // SetAlign()

            public void SetFill(int col, int fill = 1)
            {
                colFill[col] = fill;
            } // SetFill()

            public void SetBar(int col, bool bar = true)
            {
                colBar[col] = bar;
            } // SetBar()

            public void SetWidth(int col, int width)
            {
                colWidth[col] = width;
            } // SetWidth()

            public string[][] ToSpan(int width = 0, int span = 1)
            {
                int c, r, s, i, j, textwidth, unused, remaining;
                int[] colWidth;
                byte w;
                double value;
                string text;
                StringBuilder sb;
                string[][] spanLines;

                // clone the user-defined widths and tally fill columns
                colWidth = (int[])this.colWidth.Clone();
                unused = width * span - padding * SZ_SPACE;
                remaining = 0;
                for (c = 0; c < numCols; c++)
                {
                    unused -= padding * SZ_SPACE;
                    if (colFill[c] == 0)
                        unused -= colWidth[c];
                    remaining += colFill[c];
                }

                // distribute remaining width to fill columns
                for (c = 0; c < numCols & remaining > 0; c++)
                {
                    if (colFill[c] > 0)
                    {
                        colWidth[c] = Math.Max(colWidth[c], colFill[c] * unused / remaining);
                        unused -= colWidth[c];
                        remaining -= colFill[c];
                    }
                }

                // initialize output arrays
                spanLines = new string[span][];
                for (s = 0; s < span; s++)
                    spanLines[s] = new string[numRows];
                span--; // make "span" inclusive so "s < span" implies one left

                // render all rows and columns
                i = 0;
                sb = new StringBuilder();
                for (r = 0; r < numRows; r++)
                {
                    sb.Clear();
                    s = 0;
                    remaining = width;
                    unused = 0;
                    for (c = 0; c < numCols; c++)
                    {
                        unused += padding * SZ_SPACE;
                        if (r >= colRowText[c].Count || colRowText[c][r] == "")
                        {
                            unused += colWidth[c];
                        }
                        else
                        {
                            // render the bar, or fetch the cell text
                            text = colRowText[c][r];
                            charWidth.TryGetValue(text[0], out w);
                            textwidth = colRowWidth[c][r];
                            if (colBar[c] == true)
                            {
                                value = 0.0;
                                if (double.TryParse(text, out value))
                                    value = Math.Min(Math.Max(value, 0.0), 1.0);
                                i = (int)((colWidth[c] / SZ_SPACE) * value + 0.5);
                                w = SZ_SPACE;
                                textwidth = i * SZ_SPACE;
                            }

                            // if the column is not left-aligned, calculate left spacing
                            if (colAlign[c] > 0)
                            {
                                unused += (colWidth[c] - textwidth);
                            }
                            else if (colAlign[c] == 0)
                            {
                                unused += (colWidth[c] - textwidth) / 2;
                            }

                            // while the left spacing leaves no room for text, adjust it
                            while (s < span & unused > remaining - w)
                            {
                                sb.Append(' ');
                                spanLines[s][r] = sb.ToString();
                                sb.Clear();
                                s++;
                                unused -= remaining;
                                remaining = width;
                            }

                            // add left spacing
                            remaining -= unused;
                            sb.Append(Format("", unused, out unused));
                            remaining += unused;

                            // if the column is not right-aligned, calculate right spacing
                            if (colAlign[c] < 0)
                            {
                                unused += (colWidth[c] - textwidth);
                            }
                            else if (colAlign[c] == 0)
                            {
                                unused += (colWidth[c] - textwidth) - ((colWidth[c] - textwidth) / 2);
                            }

                            // while the bar or text runs to the next span, split it
                            if (colBar[c] == true)
                            {
                                while (s < span & textwidth > remaining)
                                {
                                    j = remaining / SZ_SPACE;
                                    remaining -= j * SZ_SPACE;
                                    textwidth -= j * SZ_SPACE;
                                    sb.Append(new String('I', j));
                                    spanLines[s][r] = sb.ToString();
                                    sb.Clear();
                                    s++;
                                    unused -= remaining;
                                    remaining = width;
                                    i -= j;
                                }
                                text = new String('I', i);
                            }
                            else
                            {
                                while (s < span & textwidth > remaining)
                                {
                                    i = 0;
                                    while (remaining >= w)
                                    {
                                        remaining -= w;
                                        textwidth -= w;
                                        charWidth.TryGetValue(text[++i], out w);
                                    }
                                    sb.Append(text, 0, i);
                                    spanLines[s][r] = sb.ToString();
                                    sb.Clear();
                                    s++;
                                    unused -= remaining;
                                    remaining = width;
                                    text = text.Substring(i);
                                }
                            }

                            // add cell text
                            remaining -= textwidth;
                            sb.Append(text);
                        }
                    }
                    spanLines[s][r] = sb.ToString();
                }

                return spanLines;
            } // ToSpan()

            public string ToString(int width = 0)
            {
                return String.Join("\n", ToSpan(width)[0]);
            } // ToString()

        }

        #endregion
        /*m*/
        /*-*/
    }
}
/*-*/
