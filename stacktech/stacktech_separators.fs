FeatureScript 3070;
import(path : "onshape/std/common.fs", version : "3070.0");

/*
 * StackTech Separators
 *
 * Builds 3D-printable divider bars for ToughBuilt StackTech drawers:
 *   - a full-width SIDE-TO-SIDE separator whose T-shaped ends slide down into
 *     the female edge mounts on the drawer side walls, and
 *   - FRONT-TO-BACK separators that T into the front or back wall mount and
 *     plug into vertical T-slots cut through the side-to-side separator.
 *
 * Separator construction (measured from the OEM ToughBuilt divider):
 *   - overall thickness 0.265" (perimeter rails), recessed web 0.095" thick
 *   - side-to-side wall end: T connector, neck sized to the 0.166" mount
 *     channel, head sized to the 0.29" hollow interior of the mount
 *   - front-to-back wall end (measured from the OEM large divider): a 0.21"
 *     thick end section, a ridge that bears on the outer face of the wall
 *     mount (which stands 0.285" off the wall), a neck through the mount's
 *     0.1525" channel wall, and a rib that runs in the mount's 0.15" deep channel;
 *     in face view the thin section is a vertical strip along the wall edge
 *     with chamfered top and bottom; the corners above and below it are full
 *     thickness, and the ridge/neck/rib run only over the strip's edge height
 *   - separator-to-separator joint: 0.095" web tongue through a 0.095" T-slot
 *     in the mating separator, retained by a 0.265" head on the far side
 *
 * Coordinate system: X = side to side (width), Y = front to back (depth,
 * Y = 0 at the front wall), Z = up (Z = 0 on the drawer floor).
 *
 * All drawer dimensions live in drawerSpec() below. See STACKTECH_DATA.md
 * for where each number came from and which ones are estimates.
 */

export enum StackTechDrawer
{
    annotation { "Name" : "1-Drawer Tool Box (TB-B1-D-30-1)" }
    D_30_1,
    annotation { "Name" : "XL 2-Drawer Locking Box (TB-B1-D-72), per drawer" }
    D_72,
    annotation { "Name" : "XL 3-Drawer Tool Box (TB-B1-D-70-3), per drawer" }
    D_70_3,
    annotation { "Name" : "XL 4-Drawer Locking Box (TB-B1-D-74), per drawer" }
    D_74,
    annotation { "Name" : "XL 1-Drawer Locking Box (TB-B1-D-71), deep drawer" }
    D_71,
    annotation { "Name" : "Rolling 1-Drawer Locking Box (TB-B1-D-R91)" }
    D_R91,
    annotation { "Name" : "Rolling 2-Drawer Locking Box (TB-B1-D-R92), top drawer" }
    D_R92_TOP,
    annotation { "Name" : "Rolling 2-Drawer Locking Box (TB-B1-D-R92), bottom drawer" }
    D_R92_BOTTOM
}

export enum SeparatorKind
{
    annotation { "Name" : "Side-to-side separator only" }
    SIDE_TO_SIDE,
    annotation { "Name" : "Front-to-back separators only" }
    FRONT_TO_BACK,
    annotation { "Name" : "Side-to-side and front-to-back" }
    BOTH
}

export enum FrontToBackAnchor
{
    annotation { "Name" : "Front wall to the side-to-side separator" }
    FROM_FRONT_WALL,
    annotation { "Name" : "Side-to-side separator to the back wall" }
    FROM_BACK_WALL,
    annotation { "Name" : "Between two side-to-side separators" }
    BETWEEN_SEPARATORS
}

/**
 * Interior dimensions and receiver counts per drawer.
 *   width      : side to side
 *   depth      : front to back
 *   height     : floor to rim
 *   sideSlots  : female edge mounts on each side wall, counted front to back
 *   positions  : front-to-back divider positions across the width
 *                (2 for the stock 3-column grid, 3 for the 4-column grid)
 */
function drawerSpec(drawer is StackTechDrawer) returns map
{
    if (drawer == StackTechDrawer.D_30_1)
        return { "label" : "1-Drawer Tool Box", "width" : 383.5 * millimeter, "depth" : 330 * millimeter, "height" : 114 * millimeter, "sideSlots" : 2, "positions" : 2 };
    if (drawer == StackTechDrawer.D_72)
        return { "label" : "XL 2-Drawer Locking Box", "width" : 383.5 * millimeter, "depth" : 330 * millimeter, "height" : 135 * millimeter, "sideSlots" : 2, "positions" : 2 };
    if (drawer == StackTechDrawer.D_70_3)
        return { "label" : "XL 3-Drawer Tool Box", "width" : 383.5 * millimeter, "depth" : 330 * millimeter, "height" : 89 * millimeter, "sideSlots" : 2, "positions" : 2 };
    if (drawer == StackTechDrawer.D_74)
        return { "label" : "XL 4-Drawer Locking Box", "width" : 383.5 * millimeter, "depth" : 330 * millimeter, "height" : 64 * millimeter, "sideSlots" : 2, "positions" : 3 };
    if (drawer == StackTechDrawer.D_71)
        return { "label" : "XL 1-Drawer Locking Box", "width" : 383.5 * millimeter, "depth" : 330 * millimeter, "height" : 285 * millimeter, "sideSlots" : 3, "positions" : 5 };
    if (drawer == StackTechDrawer.D_R91)
        return { "label" : "Rolling 1-Drawer Locking Box", "width" : 430 * millimeter, "depth" : 398 * millimeter, "height" : 285 * millimeter, "sideSlots" : 3, "positions" : 5 };
    if (drawer == StackTechDrawer.D_R92_TOP)
        return { "label" : "Rolling 2-Drawer Locking Box (top)", "width" : 430 * millimeter, "depth" : 398 * millimeter, "height" : 120 * millimeter, "sideSlots" : 2, "positions" : 3 };
    return { "label" : "Rolling 2-Drawer Locking Box (bottom)", "width" : 430 * millimeter, "depth" : 398 * millimeter, "height" : 273 * millimeter, "sideSlots" : 3, "positions" : 5 };
}

// End connector types
const END_NONE = "NONE";
const END_WALL = "WALL"; // T into a drawer-wall edge mount (side-to-side separators)
const END_HOOK = "HOOK"; // ridge + rib into a drawer-wall edge mount (front-to-back separators)
const END_SLOT = "SLOT"; // T through a slot in another separator

const SLOT_BOUNDS = { (unitless) : [1, 1, 8] } as IntegerBoundSpec;
const SECOND_SLOT_BOUNDS = { (unitless) : [1, 2, 8] } as IntegerBoundSpec;
const COUNT_BOUNDS = { (unitless) : [1, 1, 8] } as IntegerBoundSpec;
const POSITION_BOUNDS = { (unitless) : [1, 2, 8] } as IntegerBoundSpec;

// Measured OEM separator / mount dimensions (inches) are the defaults.
const THICKNESS_BOUNDS = { (meter) : [0.000508, 0.006731, 0.0127], (millimeter) : 6.731, (centimeter) : 0.6731, (inch) : 0.265, (foot) : 0.02208, (yard) : 0.00736 } as LengthBoundSpec;
const WEB_BOUNDS = { (meter) : [0.000254, 0.002413, 0.00762], (millimeter) : 2.413, (centimeter) : 0.2413, (inch) : 0.095, (foot) : 0.00792, (yard) : 0.00264 } as LengthBoundSpec;
const T_SLOT_BOUNDS = { (meter) : [0.000254, 0.002413, 0.00762], (millimeter) : 2.413, (centimeter) : 0.2413, (inch) : 0.095, (foot) : 0.00792, (yard) : 0.00264 } as LengthBoundSpec;
const MOUNT_HOLLOW_BOUNDS = { (meter) : [0.00127, 0.007366, 0.0254], (millimeter) : 7.366, (centimeter) : 0.7366, (inch) : 0.29, (foot) : 0.02417, (yard) : 0.00806 } as LengthBoundSpec;
const MOUNT_CHANNEL_BOUNDS = { (meter) : [0.000508, 0.0042164, 0.0127], (millimeter) : 4.2164, (centimeter) : 0.42164, (inch) : 0.166, (foot) : 0.01383, (yard) : 0.00461 } as LengthBoundSpec;
const HEAD_LENGTH_BOUNDS = { (meter) : [0.00127, 0.00635, 0.0254], (millimeter) : 6.35, (centimeter) : 0.635, (inch) : 0.25, (foot) : 0.02083, (yard) : 0.00694 } as LengthBoundSpec;
const NECK_LENGTH_BOUNDS = { (meter) : [0.000508, 0.00254, 0.0127], (millimeter) : 2.54, (centimeter) : 0.254, (inch) : 0.10, (foot) : 0.00833, (yard) : 0.00278 } as LengthBoundSpec;
const RAIL_WIDTH_BOUNDS = { (meter) : [0, 0.00635, 0.0508], (millimeter) : 6.35, (centimeter) : 0.635, (inch) : 0.25, (foot) : 0.02083, (yard) : 0.00694 } as LengthBoundSpec;
const CLEARANCE_BOUNDS = { (meter) : [0, 0.0003175, 0.00127], (millimeter) : 0.3175, (centimeter) : 0.03175, (inch) : 0.0125, (foot) : 0.00104, (yard) : 0.000347 } as LengthBoundSpec;
const TOP_CLEARANCE_BOUNDS = { (meter) : [0, 0.004, 0.1016], (millimeter) : 4, (centimeter) : 0.4, (inch) : 0.157, (foot) : 0.01312, (yard) : 0.00437 } as LengthBoundSpec;
const END_CLEARANCE_BOUNDS = { (meter) : [0, 0.000508, 0.0127], (millimeter) : 0.508, (centimeter) : 0.0508, (inch) : 0.02, (foot) : 0.00167, (yard) : 0.000556 } as LengthBoundSpec;
// Front-to-back wall connector (measured from the OEM large divider).
const FB_BUMP_OUT_BOUNDS = { (meter) : [0.00127, 0.007239, 0.0254], (millimeter) : 7.239, (centimeter) : 0.7239, (inch) : 0.285, (foot) : 0.02375, (yard) : 0.00792 } as LengthBoundSpec;
const FB_END_THICKNESS_BOUNDS = { (meter) : [0.000508, 0.005334, 0.0127], (millimeter) : 5.334, (centimeter) : 0.5334, (inch) : 0.21, (foot) : 0.0175, (yard) : 0.00583 } as LengthBoundSpec;
const FB_RIDGE_GAP_BOUNDS = { (meter) : [0.000508, 0.0038735, 0.0127], (millimeter) : 3.8735, (centimeter) : 0.38735, (inch) : 0.1525, (foot) : 0.01271, (yard) : 0.00424 } as LengthBoundSpec;
const FB_RIDGE_WIDTH_BOUNDS = { (meter) : [0.000254, 0.001524, 0.0127], (millimeter) : 1.524, (centimeter) : 0.1524, (inch) : 0.06, (foot) : 0.005, (yard) : 0.00167 } as LengthBoundSpec;
const FB_END_LENGTH_BOUNDS = { (meter) : [0, 0.00889, 0.0762], (millimeter) : 8.89, (centimeter) : 0.889, (inch) : 0.35, (foot) : 0.02917, (yard) : 0.00972 } as LengthBoundSpec;
const FB_CHANNEL_DEPTH_BOUNDS = { (meter) : [0.000508, 0.00381, 0.0127], (millimeter) : 3.81, (centimeter) : 0.381, (inch) : 0.15, (foot) : 0.0125, (yard) : 0.00417 } as LengthBoundSpec;
const FB_DROP_BOUNDS = { (meter) : [0, 0.01905, 0.0762], (millimeter) : 19.05, (centimeter) : 1.905, (inch) : 0.75, (foot) : 0.0625, (yard) : 0.02083 } as LengthBoundSpec;
const FB_CHAMFER_BOUNDS = { (meter) : [0, 0.00635, 0.0508], (millimeter) : 6.35, (centimeter) : 0.635, (inch) : 0.25, (foot) : 0.02083, (yard) : 0.00694 } as LengthBoundSpec;

annotation { "Feature Type Name" : "StackTech Separators", "Feature Type Description" : "Side-to-side and front-to-back divider bars for ToughBuilt StackTech drawers" }
export const stackTechSeparators = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Drawer" }
        definition.drawer is StackTechDrawer;

        annotation { "Name" : "Create" }
        definition.kind is SeparatorKind;

        annotation { "Name" : "Side-to-side separator slot (counted from the front wall)" }
        isInteger(definition.sideSlot, SLOT_BOUNDS);

        if (definition.kind != SeparatorKind.SIDE_TO_SIDE)
        {
            annotation { "Name" : "Front-to-back separators run" }
            definition.anchor is FrontToBackAnchor;

            if (definition.anchor == FrontToBackAnchor.BETWEEN_SEPARATORS)
            {
                annotation { "Name" : "Second side-to-side slot (counted from the front wall)" }
                isInteger(definition.secondSideSlot, SECOND_SLOT_BOUNDS);
            }

            annotation { "Name" : "Number of front-to-back separators" }
            isInteger(definition.frontToBackCount, COUNT_BOUNDS);
        }

        annotation { "Name" : "Override front-to-back positions" }
        definition.overridePositions is boolean;

        if (definition.overridePositions)
        {
            annotation { "Name" : "Front-to-back positions across the width" }
            isInteger(definition.positions, POSITION_BOUNDS);
        }

        annotation { "Name" : "Show drawer interior reference body" }
        definition.showEnvelope is boolean;

        annotation { "Group Name" : "Separator geometry", "Collapsed By Default" : true }
        {
            annotation { "Name" : "Separator thickness" }
            isLength(definition.thickness, THICKNESS_BOUNDS);

            annotation { "Name" : "Recessed web thickness" }
            isLength(definition.webThickness, WEB_BOUNDS);

            annotation { "Name" : "Separator T-slot interior" }
            isLength(definition.tSlotInterior, T_SLOT_BOUNDS);

            annotation { "Name" : "Edge mount hollow interior" }
            isLength(definition.mountHollow, MOUNT_HOLLOW_BOUNDS);

            annotation { "Name" : "Edge mount channel width" }
            isLength(definition.mountChannel, MOUNT_CHANNEL_BOUNDS);

            annotation { "Name" : "T head length" }
            isLength(definition.headLength, HEAD_LENGTH_BOUNDS);

            annotation { "Name" : "T neck length (mount wall thickness)" }
            isLength(definition.neckLength, NECK_LENGTH_BOUNDS);

            annotation { "Name" : "Rail width around the web (0 = solid plate)" }
            isLength(definition.railWidth, RAIL_WIDTH_BOUNDS);

            annotation { "Name" : "Fit clearance per side" }
            isLength(definition.clearance, CLEARANCE_BOUNDS);

            annotation { "Name" : "Clearance below the drawer rim" }
            isLength(definition.topClearance, TOP_CLEARANCE_BOUNDS);

            annotation { "Name" : "Clearance at each wall" }
            isLength(definition.endClearance, END_CLEARANCE_BOUNDS);

            annotation { "Name" : "Front-to-back: wall mount bump-out from the wall" }
            isLength(definition.fbBumpOut, FB_BUMP_OUT_BOUNDS);

            annotation { "Name" : "Front-to-back: wall end section thickness" }
            isLength(definition.fbEndThickness, FB_END_THICKNESS_BOUNDS);

            annotation { "Name" : "Front-to-back: ridge to rib gap" }
            isLength(definition.fbRidgeGap, FB_RIDGE_GAP_BOUNDS);

            annotation { "Name" : "Front-to-back: mount channel depth (rib depth)" }
            isLength(definition.fbChannelDepth, FB_CHANNEL_DEPTH_BOUNDS);

            annotation { "Name" : "Front-to-back: ridge width" }
            isLength(definition.fbRidgeWidth, FB_RIDGE_WIDTH_BOUNDS);

            annotation { "Name" : "Front-to-back: wall end section length" }
            isLength(definition.fbEndLength, FB_END_LENGTH_BOUNDS);

            annotation { "Name" : "Front-to-back: thick corner depth at the wall edge (top and bottom)" }
            isLength(definition.fbConnectorDrop, FB_DROP_BOUNDS);

            annotation { "Name" : "Front-to-back: corner chamfer rise toward the body" }
            isLength(definition.fbChamferRise, FB_CHAMFER_BOUNDS);
        }
    }
    {
        const spec = drawerSpec(definition.drawer);
        const zero = 0 * inch;

        if (definition.sideSlot > spec.sideSlots)
            throw regenError("The " ~ spec.label ~ " has only " ~ spec.sideSlots ~ " edge mounts per side wall.", ["sideSlot"]);

        const wantSS = definition.kind != SeparatorKind.FRONT_TO_BACK;
        const wantFB = definition.kind != SeparatorKind.SIDE_TO_SIDE;
        const between = wantFB && definition.anchor == FrontToBackAnchor.BETWEEN_SEPARATORS;

        var slots = [definition.sideSlot];
        if (between)
        {
            if (definition.secondSideSlot <= definition.sideSlot)
                throw regenError("The second slot must be further back than the first slot.", ["secondSideSlot"]);
            if (definition.secondSideSlot > spec.sideSlots)
                throw regenError("The " ~ spec.label ~ " has only " ~ spec.sideSlots ~ " edge mounts per side wall.", ["secondSideSlot"]);
            slots = append(slots, definition.secondSideSlot);
        }

        const positions = definition.overridePositions ? definition.positions : spec.positions;
        const T = definition.thickness;
        const web = definition.webThickness;
        const c = definition.clearance;
        const h = spec.height - definition.topClearance;
        if (h <= T)
            throw regenError("Rim clearance leaves no separator height.", ["topClearance"]);
        if (web >= T)
            throw regenError("The web must be thinner than the separator.", ["webThickness"]);

        // Wall T: head fills the mount hollow, neck fills the mount channel.
        const wallHeadT = min(T, definition.mountHollow - 2 * c);
        const wallNeckT = min(T, definition.mountChannel - 2 * c);
        if (wallNeckT <= zero || wallHeadT <= zero)
            throw regenError("Edge mount dimensions are smaller than twice the fit clearance.", ["mountChannel"]);
        // Separator T-slot: the mating separator's web passes through a slot in this one.
        const slotWidth = definition.tSlotInterior + 2 * c;
        const slotNeckT = min(T, definition.tSlotInterior);
        const slotNeckL = T + 2 * c;
        const endLen = definition.neckLength + definition.headLength;
        // Front-to-back wall hook: the ridge's outer face sits at the mount bump-out,
        // the neck spans the ridge-to-rib gap, and the rib runs the full depth of
        // the mount's channel (measured, so it may reach slightly past the wall plane).
        const ec = definition.endClearance;
        const fbRibDepth = definition.fbChannelDepth;
        const fbWallLen = definition.fbBumpOut + definition.fbRidgeWidth + definition.fbEndLength;

        const pitch = spec.depth / (spec.sideSlots + 1);
        const colPitch = spec.width / (positions + 1);

        const common = {
            "height" : h,
            "thickness" : T,
            "webThickness" : web,
            "railWidth" : definition.railWidth,
            "wallNeckThickness" : wallNeckT,
            "wallNeckLength" : definition.neckLength,
            "wallHeadThickness" : wallHeadT,
            "slotNeckThickness" : slotNeckT,
            "slotNeckLength" : slotNeckL,
            "slotHeadThickness" : T,
            "headLength" : definition.headLength,
            "faceSlots" : [],
            "faceSlotWidth" : slotWidth,
            // T-slots stay closed at the bottom so the separator remains one piece;
            // the mating tongue starts just above this floor.
            "slotFloor" : max(definition.railWidth, 0.1 * inch),
            "clearance" : c,
            "fbEndThickness" : min(definition.fbEndThickness, T),
            "fbEndLength" : definition.fbEndLength,
            "fbRidgeWidth" : definition.fbRidgeWidth,
            "fbRidgeGap" : definition.fbRidgeGap,
            "fbRibDepth" : fbRibDepth,
            "fbRibThickness" : min(definition.fbEndThickness, wallHeadT),
            // Thick corners at the wall edge, top and bottom, with a chamfer down to
            // the thin strip that engages the wall mount.
            "fbConnectorDrop" : definition.fbConnectorDrop,
            "fbChamferRise" : definition.fbChamferRise,
            "fbWallLen" : fbWallLen,
            "endClearance" : ec,
            "zDir" : vector(0, 0, 1)
        };
        if (h - 2 * (definition.fbConnectorDrop + definition.fbChamferRise) <= 0.2 * inch)
            throw regenError("The thick corners leave no thin section for the wall mount.", ["fbConnectorDrop"]);

        if (definition.showEnvelope)
        {
            fCuboid(context, id + "envelope", {
                "corner1" : vector(zero, zero, zero),
                "corner2" : vector(spec.width, spec.depth, spec.height)
            });
            setProperty(context, {
                "entities" : qCreatedBy(id + "envelope", EntityType.BODY),
                "propertyType" : PropertyType.NAME,
                "value" : spec.label ~ " interior (reference)"
            });
        }

        if (wantSS)
        {
            const xStart = ec + endLen;
            const bodyLength = spec.width - 2 * xStart;
            if (bodyLength <= 2 * definition.railWidth)
                throw regenError("Side-to-side separator body would be too short.", ["headLength"]);
            var faceSlots = [];
            for (var j = 1; j <= positions; j += 1)
                faceSlots = append(faceSlots, j * colPitch - xStart);
            for (var k in slots)
            {
                createSeparator(context, id + ("ss" ~ k), mergeMaps(common, {
                    "origin" : vector(xStart, k * pitch, zero),
                    "uDir" : vector(1, 0, 0),
                    "vDir" : vector(0, 1, 0),
                    "bodyLength" : bodyLength,
                    "leftEnd" : END_WALL,
                    "rightEnd" : END_WALL,
                    "faceSlots" : faceSlots,
                    "name" : "StackTech side-to-side separator - " ~ spec.label ~ " - slot " ~ k
                }));
            }
        }

        if (wantFB)
        {
            const count = min(definition.frontToBackCount, positions);
            const yFirst = slots[0] * pitch;
            var yStart;
            var bodyLength;
            var leftEnd;
            var rightEnd;
            var runLabel;
            if (definition.anchor == FrontToBackAnchor.FROM_FRONT_WALL)
            {
                yStart = fbWallLen;
                bodyLength = (yFirst - T / 2 - c) - yStart;
                leftEnd = END_HOOK;
                rightEnd = END_SLOT;
                runLabel = "front wall to slot " ~ slots[0];
            }
            else if (definition.anchor == FrontToBackAnchor.FROM_BACK_WALL)
            {
                yStart = yFirst + T / 2 + c;
                bodyLength = (spec.depth - fbWallLen) - yStart;
                leftEnd = END_SLOT;
                rightEnd = END_HOOK;
                runLabel = "slot " ~ slots[0] ~ " to back wall";
            }
            else
            {
                yStart = yFirst + T / 2 + c;
                bodyLength = (slots[1] * pitch - T / 2 - c) - yStart;
                leftEnd = END_SLOT;
                rightEnd = END_SLOT;
                runLabel = "slot " ~ slots[0] ~ " to slot " ~ slots[1];
            }
            if (bodyLength <= 2 * definition.railWidth + fbWallLen)
                throw regenError("Front-to-back separator would be too short for its connectors.", ["sideSlot"]);

            for (var j = 1; j <= count; j += 1)
            {
                createSeparator(context, id + ("fb" ~ j), mergeMaps(common, {
                    "origin" : vector(j * colPitch, yStart, zero),
                    "uDir" : vector(0, 1, 0),
                    "vDir" : vector(1, 0, 0),
                    "bodyLength" : bodyLength,
                    "leftEnd" : leftEnd,
                    "rightEnd" : rightEnd,
                    "name" : "StackTech front-to-back separator - " ~ spec.label ~ " - " ~ runLabel ~ " - position " ~ j
                }));
            }
        }
    }, {
        "drawer" : StackTechDrawer.D_30_1,
        "kind" : SeparatorKind.SIDE_TO_SIDE,
        "sideSlot" : 1,
        "anchor" : FrontToBackAnchor.FROM_FRONT_WALL,
        "secondSideSlot" : 2,
        "frontToBackCount" : 1,
        "overridePositions" : false,
        "positions" : 2,
        "showEnvelope" : false,
        "thickness" : 0.265 * inch,
        "webThickness" : 0.095 * inch,
        "tSlotInterior" : 0.095 * inch,
        "mountHollow" : 0.29 * inch,
        "mountChannel" : 0.166 * inch,
        "headLength" : 0.25 * inch,
        "neckLength" : 0.10 * inch,
        "railWidth" : 0.25 * inch,
        "clearance" : 0.0125 * inch,
        "topClearance" : 0.157 * inch,
        "endClearance" : 0.02 * inch,
        "fbBumpOut" : 0.285 * inch,
        "fbEndThickness" : 0.21 * inch,
        "fbRidgeGap" : 0.1525 * inch,
        "fbRidgeWidth" : 0.06 * inch,
        "fbEndLength" : 0.35 * inch,
        "fbChannelDepth" : 0.15 * inch,
        "fbConnectorDrop" : 0.75 * inch,
        "fbChamferRise" : 0.25 * inch
    });

/**
 * Builds one separator as a single body.
 * Local coordinates: u along the separator (0 at the body start, p.bodyLength at
 * the body end), v across the thickness (0 on the mid-plane), z up from the floor.
 *   p.origin / p.uDir / p.vDir / p.zDir : placement
 *   p.leftEnd / p.rightEnd              : END_NONE, END_WALL, END_HOOK or END_SLOT
 *   p.faceSlots                         : u positions of T-slots cut through this separator
 */
function createSeparator(context is Context, id is Id, p is map)
{
    const zero = 0 * inch;
    const eps = 0.01 * inch;
    const L = p.bodyLength;
    const h = p.height;
    const T = p.thickness;
    const rw = p.railWidth;

    var pieces = [id + "body"];
    localCuboid(context, id + "body", p, zero, L, -T / 2, T / 2, zero, h);

    // Recess both faces down to the web, leaving perimeter rails.
    if (rw > zero && L > 2 * rw + eps && h > 2 * rw + eps)
    {
        localCuboid(context, id + "recessA", p, rw, L - rw, p.webThickness / 2, T / 2 + eps, rw, h - rw);
        localCuboid(context, id + "recessB", p, rw, L - rw, -T / 2 - eps, -p.webThickness / 2, rw, h - rw);
        opBoolean(context, id + "recess", {
            "tools" : qUnion([qCreatedBy(id + "recessA", EntityType.BODY), qCreatedBy(id + "recessB", EntityType.BODY)]),
            "targets" : qCreatedBy(id + "body", EntityType.BODY),
            "operationType" : BooleanOperationType.SUBTRACTION
        });
    }

    pieces = concatenateArrays([pieces, endConnector(context, id + "left", p, p.leftEnd, -1)]);
    pieces = concatenateArrays([pieces, endConnector(context, id + "right", p, p.rightEnd, 1)]);

    var queries = [];
    for (var pieceId in pieces)
        queries = append(queries, qCreatedBy(pieceId, EntityType.BODY));
    const bodyQuery = qUnion(queries);

    if (size(pieces) > 1)
    {
        opBoolean(context, id + "union", {
            "tools" : bodyQuery,
            "operationType" : BooleanOperationType.UNION
        });
    }

    // Vertical T-slots through the full thickness for mating separators,
    // open at the top and closed at the slot floor.
    if (size(p.faceSlots) > 0)
    {
        var cuts = [];
        for (var i = 0; i < size(p.faceSlots); i += 1)
        {
            const u = p.faceSlots[i];
            const cutId = id + ("slot" ~ i);
            localCuboid(context, cutId, p, u - p.faceSlotWidth / 2, u + p.faceSlotWidth / 2, -T / 2 - eps, T / 2 + eps, p.slotFloor, h + eps);
            cuts = append(cuts, qCreatedBy(cutId, EntityType.BODY));
        }
        opBoolean(context, id + "slots", {
            "tools" : qUnion(cuts),
            "targets" : bodyQuery,
            "operationType" : BooleanOperationType.SUBTRACTION
        });
    }

    setProperty(context, {
        "entities" : bodyQuery,
        "propertyType" : PropertyType.NAME,
        "value" : p.name
    });
}

/**
 * Adds a connector to one end of a separator.
 * dir = -1 for the u = 0 end, +1 for the u = bodyLength end.
 * Returns the ids of the pieces created so the caller can union them.
 *
 * END_WALL / END_SLOT : T connector (neck + head).
 * END_HOOK            : front-to-back wall connector, from the body outward:
 *                       thin strip (chamfered top/bottom, thick corners above
 *                       and below) -> full-thickness ridge (bears on the mount's
 *                       outer face) -> neck through the mount's channel wall ->
 *                       rib riding inside the mount.
 */
function endConnector(context is Context, id is Id, p is map, endType is string, dir is number) returns array
{
    if (endType == END_NONE)
        return [];
    const zero = 0 * inch;
    const overlap = 0.02 * inch;
    const base = dir < 0 ? zero : p.bodyLength;

    if (endType == END_HOOK)
    {
        // Face view of the OEM wall end: the thin (0.21") section is a vertical
        // strip along the wall edge whose top and bottom are cut on a diagonal
        // (outer edge shallower, inner edge deeper); the corners above and below
        // it are full thickness. The ridge, neck and rib that engage the wall
        // mount run only over the height where the thin strip reaches the edge.
        const tEnd = p.fbEndThickness;
        const T = p.thickness;
        const h = p.height;
        const uS = p.fbEndLength;                           // thin strip outer face / ridge inner face
        const uRidgeOut = uS + p.fbRidgeWidth;              // ridge outer face (mount bump-out)
        const uRib = uRidgeOut + p.fbRidgeGap;              // rib inner face (inside the mount)
        const uRibTip = uRib + p.fbRibDepth;                // rib tip at the bottom of the mount channel
        const d1 = p.fbConnectorDrop;                       // thick corner depth at the outer edge
        const d2 = d1 + p.fbChamferRise;                    // thick corner depth at the body junction
        const z0 = d1;                                      // mount engagement range
        const z1 = h - d1;

        // Thin strip: hexagon with chamfered top and bottom, overlapping the body
        // and the ridge by `overlap`.
        uzPrism(context, id + "end", p, base, dir, [
            [-overlap, d2], [uS + overlap, d1], [uS + overlap, z1], [-overlap, h - d2]
        ], tEnd);
        // Thick corner blocks above and below the strip, dipping `overlap` past the
        // chamfer so the union shares volume with the strip.
        uzPrism(context, id + "top", p, base, dir, [
            [-overlap, h], [uS, h], [uS, z1 - overlap], [-overlap, h - d2 - overlap]
        ], T);
        uzPrism(context, id + "bottom", p, base, dir, [
            [-overlap, zero], [uS, zero], [uS, d1 + overlap], [-overlap, d2 + overlap]
        ], T);
        // Ridge, neck and rib into the wall mount.
        localCuboid(context, id + "ridge", p, base + dir * uS, base + dir * uRidgeOut, -T / 2, T / 2, z0, z1);
        localCuboid(context, id + "neck", p, base + dir * (uRidgeOut - overlap), base + dir * (uRib + overlap), -p.wallNeckThickness / 2, p.wallNeckThickness / 2, z0, z1);
        localCuboid(context, id + "rib", p, base + dir * uRib, base + dir * uRibTip, -p.fbRibThickness / 2, p.fbRibThickness / 2, z0, z1);
        return [id + "end", id + "top", id + "bottom", id + "ridge", id + "neck", id + "rib"];
    }

    const neckT = endType == END_WALL ? p.wallNeckThickness : p.slotNeckThickness;
    const neckL = endType == END_WALL ? p.wallNeckLength : p.slotNeckLength;
    const headT = endType == END_WALL ? p.wallHeadThickness : p.slotHeadThickness;
    const headL = p.headLength;
    // A tongue that drops into another separator's T-slot stops above the slot floor.
    const z0 = endType == END_WALL ? zero : p.slotFloor + p.clearance;

    localCuboid(context, id + "neck", p, base - dir * overlap, base + dir * (neckL + overlap), -neckT / 2, neckT / 2, z0, p.height);
    localCuboid(context, id + "head", p, base + dir * neckL, base + dir * (neckL + headL), -headT / 2, headT / 2, z0, p.height);
    return [id + "neck", id + "head"];
}

/**
 * Prism in the separator's u/z plane, centred on the mid-plane and `thickness`
 * thick. `pts` are [u, z] pairs with u measured from `base` in the outward
 * direction `dir` (u > 0 is away from the body).
 */
function uzPrism(context is Context, id is Id, p is map, base, dir is number, pts is array, thickness)
{
    const normal = cross(p.uDir, p.zDir);
    const origin = p.origin + base * p.uDir - (thickness / 2) * normal;
    const sketch = newSketchOnPlane(context, id + "sketch", {
        "sketchPlane" : plane(origin, normal, p.uDir)
    });
    var points = [];
    for (var pt in pts)
        points = append(points, vector(dir * pt[0], pt[1]));
    points = append(points, points[0]);
    skPolyline(sketch, "profile", { "points" : points });
    skSolve(sketch);
    opExtrude(context, id, {
        "entities" : qSketchRegion(id + "sketch"),
        "direction" : normal,
        "endBound" : BoundingType.BLIND,
        "endDepth" : thickness
    });
    opDeleteBodies(context, id + "deleteSketch", {
        "entities" : qCreatedBy(id + "sketch", EntityType.BODY)
    });
}

/** Axis-aligned cuboid given in the separator's local u/v/z coordinates. */
function localCuboid(context is Context, id is Id, p is map, u0, u1, v0, v1, z0, z1)
{
    const c1 = p.origin + u0 * p.uDir + v0 * p.vDir + z0 * p.zDir;
    const c2 = p.origin + u1 * p.uDir + v1 * p.vDir + z1 * p.zDir;
    fCuboid(context, id, {
        "corner1" : vector(min(c1[0], c2[0]), min(c1[1], c2[1]), min(c1[2], c2[2])),
        "corner2" : vector(max(c1[0], c2[0]), max(c1[1], c2[1]), max(c1[2], c2[2]))
    });
}
