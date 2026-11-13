FeatureScript 3070;
import(path : "onshape/std/common.fs", version : "3070.0");

/*
 * StackTech Separators
 *
 * Builds 3D-printable divider bars for ToughBuilt StackTech drawers:
 *   - a full-width SIDE-TO-SIDE separator whose ends slide down over the
 *     female edge mounts on the drawer side walls, and
 *   - FRONT-TO-BACK separators that hook into the front or back wall mount and
 *     plug into vertical T-slots cut through the side-to-side separator.
 *
 * The end of each separator is a fixed design, taken from two STEP samples
 * (Side to Side Edge Geometry and Front to Back Geometry, 2026-11) and held in
 * SS_EDGE and FB_EDGE below. Nothing in either edge depends on the drawer: the
 * drawer sets how long the plain span between the ends is and, where it lists
 * a measured separator height, how tall the plain web is. Every rail, neck,
 * hook, rib and chamfer keeps its size; a taller or shorter part only
 * lengthens or shortens the web and the uprights between them.
 *
 * Side-to-side separator, 3.2565" tall in the sample and 0.268" thick over a
 * 0.105" web:
 *   - a 0.25" rail along the top edge; the web runs out to the bottom edge
 *   - wall end, from the tip inward: a 0.06" head, a 0.105" neck at web
 *     thickness that the mount's channel wall slides up, and a 0.157" rail.
 *     The neck is open at the bottom and stops in a full round under the top
 *     rail, so the separator hangs on the top of the mount
 *   - a chamfer on the bottom corner of each wall end, 0.165" along the bar and
 *     0.236" up, taking the head and neck with it
 *
 * Front-to-back separator, 2.3642" tall in the sample and 0.42" thick over a
 * 0.105" web:
 *   - a 0.125" rail along the top edge
 *   - wall end: the 0.125" end rail runs down the tip for 0.879", then steps
 *     back 0.5" along a diagonal and carries on down to the bottom edge. Below
 *     the diagonal the web reaches on out to the tip and carries a tapered
 *     0.21" thick rib, which rides in the wall mount
 *   - a chamfer on the bottom corner of the wall end, 0.5" along the bar and
 *     0.3" up
 *
 * Both: a 0.0075" fillet breaks every edge of the outer faces.
 *
 * Between the ends:
 *   - the side-to-side separator prints as two parts joined tongue and groove,
 *     in either of two styles. FLUSH cuts the groove back into the bar's end,
 *     leaving a thin skin either side but keeping both faces flat. PROUD stops
 *     the bar at the seat face and builds two sleeve walls out from it, so the
 *     bar keeps its full section at the splice and the walls can be as thick as
 *     wanted, at the cost of standing out from the faces. Either way the mating
 *     part carries a full-height tongue the thickness of the web, and assembled
 *     length, height and slot positions are unchanged.
 *   - separator-to-separator joint: a tongue into a T-slot in the mating
 *     separator, stopping at that separator's mid-plane so a front-to-back run
 *     in the facing cell can enter the same slot from the other side
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

export enum SplitJointStyle
{
    annotation { "Name" : "Groove cut into the separator (flush faces)" }
    FLUSH,
    annotation { "Name" : "Groove built out from the separator (protruding sleeve)" }
    PROUD
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
 *   width            : side to side
 *   depth            : front to back
 *   height           : floor to rim
 *   sideSlots        : female edge mounts on each side wall, counted front to
 *                      back, evenly spaced between the front and back walls
 *   positions        : front-to-back divider positions across the width, which
 *                      is also the number of T-slots in the side-to-side
 *                      separator, evenly spaced between the side walls
 * Drawers measured for this feature also list (2026-11):
 *   sideToSideLength : the side-to-side separator tip to tip. Stands in for
 *                      width, which becomes this plus a wall clearance each side
 *   separatorHeight  : height of every separator in the drawer, in place of the
 *                      STEP samples' own heights
 */
function drawerSpec(drawer is StackTechDrawer) returns map
{
    if (drawer == StackTechDrawer.D_30_1)
        return { "label" : "1-Drawer Tool Box", "width" : 383.5 * millimeter, "depth" : 330 * millimeter, "height" : 114 * millimeter, "sideSlots" : 2, "positions" : 2 };
    if (drawer == StackTechDrawer.D_72)
        return { "label" : "XL 2-Drawer Locking Box", "sideToSideLength" : 15.25 * inch, "depth" : 13.125 * inch, "height" : 135 * millimeter, "separatorHeight" : 4.875 * inch, "sideSlots" : 2, "positions" : 1 };
    if (drawer == StackTechDrawer.D_70_3)
        return { "label" : "XL 3-Drawer Tool Box", "width" : 383.5 * millimeter, "depth" : 330 * millimeter, "height" : 89 * millimeter, "sideSlots" : 2, "positions" : 2 };
    if (drawer == StackTechDrawer.D_74)
        return { "label" : "XL 4-Drawer Locking Box", "sideToSideLength" : 15.25 * inch, "depth" : 13 * inch, "height" : 64 * millimeter, "separatorHeight" : 2.25 * inch, "sideSlots" : 3, "positions" : 5 };
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
const END_WALL = "WALL"; // side-to-side wall end: head, neck and rail over a drawer-wall edge mount
const END_HOOK = "HOOK"; // front-to-back wall end: set-back rail and rib into a drawer-wall edge mount
const END_SLOT = "SLOT"; // tongue into a T-slot in another separator
const END_TONGUE = "TONGUE"; // male half of the side-to-side split joint
const END_GROOVE = "GROOVE"; // female half: a raised sleeve, two walls proud of the faces

/**
 * Side-to-side edge geometry, from the Side to Side Edge Geometry STEP sample.
 * Lengths along the bar are measured in from the tip of a wall end.
 */
const SS_EDGE = {
        "height" : 3.2565 * inch,
        "thickness" : 0.268 * inch, // through the rails
        "webThickness" : 0.105 * inch,
        "topRail" : 0.25 * inch, // height of the rail along the top edge
        "endRail" : 0.157 * inch, // width of the rail that closes the web at each end
        "wallHead" : 0.06 * inch, // full-thickness head at the tip
        "wallNeck" : 0.105 * inch, // web-thickness neck between the head and the end rail
        "chamferRun" : 0.165 * inch, // bottom corner chamfer, along the bar
        "chamferRise" : 0.236 * inch // and up the tip
    };

/**
 * Front-to-back edge geometry, from the Front to Back Geometry STEP sample.
 * Lengths along the bar are measured in from the tip of the wall end.
 */
const FB_EDGE = {
        "height" : 2.3642 * inch,
        "thickness" : 0.42 * inch, // through the rails
        "webThickness" : 0.105 * inch,
        "topRail" : 0.125 * inch, // height of the rail along the top edge
        "endRail" : 0.125 * inch, // width of the end rail, upright and along the diagonal alike
        "hookUpperDrop" : 0.879 * inch, // top edge down to where the tip turns into the diagonal
        "hookSetBack" : 0.5 * inch, // how far the lower rail sits back from the tip
        "hookDiagonalDrop" : 0.35 * inch, // fall of the diagonal over that set-back
        "ribStart" : 0.0725 * inch, // rib's face nearest the tip
        "ribEndTop" : 0.2025 * inch, // rib's far face where it meets the diagonal
        "ribEndBottom" : 0.159 * inch, // and where it meets the chamfer: the rib tapers downward
        "ribThickness" : 0.21 * inch,
        "chamferRun" : 0.5 * inch, // bottom corner chamfer, along the bar
        "chamferRise" : 0.3 * inch // and up the tip
    };

// Fillet on every edge of the outer faces, in both STEP samples.
const EDGE_FILLET = 0.0075 * inch;
// T-slots stay closed below this height so the slotted separator remains one piece.
const SLOT_FLOOR = 0.25 * inch;

const SLOT_BOUNDS = { (unitless) : [1, 1, 8] } as IntegerBoundSpec;
const SECOND_SLOT_BOUNDS = { (unitless) : [1, 2, 8] } as IntegerBoundSpec;
const COUNT_BOUNDS = { (unitless) : [1, 1, 8] } as IntegerBoundSpec;
const POSITION_BOUNDS = { (unitless) : [1, 2, 8] } as IntegerBoundSpec;

const T_SLOT_BOUNDS = { (meter) : [0.000254, 0.002413, 0.00762], (millimeter) : 2.413, (centimeter) : 0.2413, (inch) : 0.095, (foot) : 0.00792, (yard) : 0.00264 } as LengthBoundSpec;
const CLEARANCE_BOUNDS = { (meter) : [0, 0.0003175, 0.00127], (millimeter) : 0.3175, (centimeter) : 0.03175, (inch) : 0.0125, (foot) : 0.00104, (yard) : 0.000347 } as LengthBoundSpec;
const END_CLEARANCE_BOUNDS = { (meter) : [0, 0.000508, 0.0127], (millimeter) : 0.508, (centimeter) : 0.0508, (inch) : 0.02, (foot) : 0.00167, (yard) : 0.000556 } as LengthBoundSpec;
// Printing limit and the sliding joint that splits the side-to-side separator.
const MAX_PRINT_BOUNDS = { (meter) : [0.05, 0.256, 2], (millimeter) : 256, (centimeter) : 25.6, (inch) : 10.07874, (foot) : 0.8399, (yard) : 0.27997 } as LengthBoundSpec;
const JOINT_DEPTH_BOUNDS = { (meter) : [0.00635, 0.0254, 0.1016], (millimeter) : 25.4, (centimeter) : 2.54, (inch) : 1, (foot) : 0.08333, (yard) : 0.02778 } as LengthBoundSpec;
const JOINT_WALL_BOUNDS = { (meter) : [0.0005, 0.00381, 0.0127], (millimeter) : 3.81, (centimeter) : 0.381, (inch) : 0.15, (foot) : 0.0125, (yard) : 0.00417 } as LengthBoundSpec;

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

        if (definition.kind != SeparatorKind.FRONT_TO_BACK)
        {
            annotation { "Name" : "Split the side-to-side separator into two printable parts" }
            definition.splitForPrinting is boolean;

            if (definition.splitForPrinting)
            {
                annotation { "Name" : "Split joint style" }
                definition.jointStyle is SplitJointStyle;
            }
        }

        annotation { "Name" : "Fillet the outer edges (0.0075 in)", "Default" : true }
        definition.filletEdges is boolean;

        annotation { "Name" : "Show drawer interior reference body" }
        definition.showEnvelope is boolean;

        annotation { "Group Name" : "Fit and joints", "Collapsed By Default" : true }
        {
            annotation { "Name" : "Clearance at each wall" }
            isLength(definition.endClearance, END_CLEARANCE_BOUNDS);

            annotation { "Name" : "Fit clearance per side" }
            isLength(definition.clearance, CLEARANCE_BOUNDS);

            annotation { "Name" : "Separator T-slot interior" }
            isLength(definition.tSlotInterior, T_SLOT_BOUNDS);

            annotation { "Name" : "Maximum printed part length" }
            isLength(definition.maxPrintLength, MAX_PRINT_BOUNDS);

            annotation { "Name" : "Split joint: slot depth (how far the tongue engages)" }
            isLength(definition.jointDepth, JOINT_DEPTH_BOUNDS);

            annotation { "Name" : "Split joint: protruding sleeve wall thickness" }
            isLength(definition.jointWallThickness, JOINT_WALL_BOUNDS);
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
        const c = definition.clearance;
        const ec = definition.endClearance;
        const ssT = SS_EDGE.thickness;
        const ssWeb = SS_EDGE.webThickness;
        // Separator T-slot: the mating separator's tongue drops into a slot in this one.
        const slotWidth = definition.tSlotInterior + 2 * c;

        // A measured side-to-side length fixes the separator tip to tip; the
        // drawer is then that plus a wall clearance at each end.
        const width = spec.sideToSideLength != undefined ? spec.sideToSideLength + 2 * ec : spec.width;
        const pitch = spec.depth / (spec.sideSlots + 1);
        const colPitch = width / (positions + 1);

        // Everything a separator needs besides its own edge geometry and placement.
        const shared = {
                "filletRadius" : definition.filletEdges ? EDGE_FILLET : zero,
                "clearance" : c,
                "faceSlots" : [],
                "faceSlotWidth" : slotWidth,
                "slotFloor" : SLOT_FLOOR,
                "slotNeckThickness" : min(FB_EDGE.webThickness, definition.tSlotInterior),
                // A tongue reaches only to the mating separator's mid-plane, so a
                // front-to-back run in the facing cell can share the same slot.
                "slotEngageDepth" : ssT / 2,
                // Tongue-and-groove joint that splits the side-to-side separator for
                // printing. The tongue is the thickness of the web. The groove is
                // either cut back into the bar (FLUSH) or built out of it as the gap
                // between two added sleeve walls (PROUD).
                "jointDepth" : definition.jointDepth,
                "jointTongueThickness" : ssWeb,
                "jointGrooveHalf" : ssWeb / 2 + c,
                "jointSleeveHalf" : ssWeb / 2 + c + definition.jointWallThickness,
                "grooveEnd" : 0,
                "zDir" : vector(0, 0, 1),
                "featureId" : id
            };
        // A drawer measured for this feature sets the height of every separator
        // in it; otherwise each keeps its STEP sample's height.
        var ss = mergeMaps(shared, SS_EDGE);
        var fb = mergeMaps(shared, FB_EDGE);
        if (spec.separatorHeight != undefined)
        {
            ss.height = spec.separatorHeight;
            fb.height = spec.separatorHeight;
        }

        // The edges keep their size, so a part can only be made so short: the
        // side-to-side neck has to close under the top rail clear of the
        // chamfer, and the front-to-back diagonal has to land clear of its.
        if (wantSS && ss.height - ss.topRail - ss.wallNeck / 2 <= ss.chamferRise)
            throw regenError("The " ~ spec.label ~ " separator height is too short for the side-to-side edge geometry.");
        if (wantFB && hookOuter(fb, fb.hookSetBack) <= fb.chamferRise)
            throw regenError("The " ~ spec.label ~ " separator height is too short for the front-to-back edge geometry.");

        if ((wantSS && ss.height > spec.height) || (wantFB && fb.height > spec.height))
            reportFeatureInfo(context, id, "A separator is taller than the " ~ spec.label ~ " interior listed in drawerSpec().");

        if (definition.showEnvelope)
        {
            fCuboid(context, id + "envelope", {
                "corner1" : vector(zero, zero, zero),
                "corner2" : vector(width, spec.depth, spec.height)
            });
            setProperty(context, {
                "entities" : qCreatedBy(id + "envelope", EntityType.BODY),
                "propertyType" : PropertyType.NAME,
                "value" : spec.label ~ " interior (reference)"
            });
        }

        if (wantSS)
        {
            // Tip to tip: the wall ends are part of the bar, not added to it.
            const xStart = ec;
            const xEnd = width - ec;
            const wallLen = ss.wallHead + ss.wallNeck + ss.endRail;
            if (xEnd - xStart <= 2 * wallLen)
                throw regenError("The drawer is too narrow for the side-to-side separator's two wall ends.", ["endClearance"]);

            // Absolute X of every front-to-back T-slot in the bar.
            var slotXs = [];
            for (var j = 1; j <= positions; j += 1)
                slotXs = append(slotXs, j * colPitch);

            if (!definition.splitForPrinting)
            {
                for (var k in slots)
                {
                    createSeparator(context, id + ("ss" ~ k), mergeMaps(ss, {
                        "origin" : vector(xStart, k * pitch, zero),
                        "uDir" : vector(1, 0, 0),
                        "vDir" : vector(0, 1, 0),
                        "bodyLength" : xEnd - xStart,
                        "leftEnd" : END_WALL,
                        "rightEnd" : END_WALL,
                        "faceSlots" : localSlots(slotXs, xStart, xStart, xEnd),
                        "name" : "StackTech side-to-side separator - " ~ spec.label ~ " - slot " ~ k
                    }));
                }
            }
            else
            {
                // The joint region, which runs jointDepth back from the joint plane,
                // has to clear every T-slot and both wall ends, so the split lands in
                // whichever gap between slots comes nearest the middle of the bar.
                const split = splitPosition(slotXs, xStart + wallLen, xEnd - wallLen, definition.jointDepth,
                    slotWidth / 2, ss.endRail);
                if (!split.found)
                    throw regenError("No gap between the front-to-back slots is wide enough for the split joint. Reduce the slot depth.", ["jointDepth"]);
                const xJoint = split.x;

                // The groove is the tongue's width plus fit clearance. Either
                // style needs material left either side of it.
                if (ssWeb / 2 + c >= ssT / 2)
                    throw regenError("The split joint's groove is as wide as the separator. Reduce the fit clearance.", ["clearance"]);

                const proud = definition.jointStyle == SplitJointStyle.PROUD;

                // PROUD: part 1's body stops at the seat face, one slot depth short
                // of the joint plane, and its sleeve walls carry on from there.
                // FLUSH: part 1's body runs to the joint plane and the groove is
                // cut back into it.
                const xSeat = xJoint - definition.jointDepth;
                const partABodyEnd = proud ? xSeat : xJoint;

                // Printed length of each part: wall tip to the joint, and tongue
                // tip to wall tip. Assembled, the two still span the same width.
                const partALength = (proud ? xJoint - c : xJoint) - xStart;
                const partBLength = xEnd - xSeat;
                if (max(partALength, partBLength) > definition.maxPrintLength)
                    throw regenError("Even split in two the side-to-side separator is longer than the maximum printed length. Raise the limit or split it by hand.", ["maxPrintLength"]);

                // A sleeve roots on the plain end face. A cut groove needs solid
                // material around it, which createSeparator leaves at that end.
                const partAJoint = proud
                    ? { "rightEnd" : END_GROOVE }
                    : { "rightEnd" : END_NONE, "grooveEnd" : 1 };

                for (var k in slots)
                {
                    createSeparator(context, id + ("ssA" ~ k), mergeMaps(mergeMaps(ss, partAJoint), {
                        "origin" : vector(xStart, k * pitch, zero),
                        "uDir" : vector(1, 0, 0),
                        "vDir" : vector(0, 1, 0),
                        "bodyLength" : partABodyEnd - xStart,
                        "leftEnd" : END_WALL,
                        "faceSlots" : localSlots(slotXs, xStart, xStart, partABodyEnd),
                        "name" : "StackTech side-to-side separator - " ~ spec.label ~ " - slot " ~ k ~ " - part 1 of 2 (" ~ (proud ? "groove sleeve" : "groove") ~ ")"
                    }));
                    createSeparator(context, id + ("ssB" ~ k), mergeMaps(ss, {
                        "origin" : vector(xJoint, k * pitch, zero),
                        "uDir" : vector(1, 0, 0),
                        "vDir" : vector(0, 1, 0),
                        "bodyLength" : xEnd - xJoint,
                        "leftEnd" : END_TONGUE,
                        "rightEnd" : END_WALL,
                        "faceSlots" : localSlots(slotXs, xJoint, xJoint, xEnd),
                        "name" : "StackTech side-to-side separator - " ~ spec.label ~ " - slot " ~ k ~ " - part 2 of 2 (tongue)"
                    }));
                }
            }
        }

        if (wantFB)
        {
            const count = min(definition.frontToBackCount, positions);
            const yFirst = slots[0] * pitch;
            // A run stops a clearance short of the side-to-side separator's face;
            // its wall end starts a wall clearance off the drawer wall.
            const ssFace = ssT / 2 + c;
            var yStart;
            var bodyLength;
            var leftEnd;
            var rightEnd;
            var runLabel;
            if (definition.anchor == FrontToBackAnchor.FROM_FRONT_WALL)
            {
                yStart = ec;
                bodyLength = (yFirst - ssFace) - yStart;
                leftEnd = END_HOOK;
                rightEnd = END_SLOT;
                runLabel = "front wall to slot " ~ slots[0];
            }
            else if (definition.anchor == FrontToBackAnchor.FROM_BACK_WALL)
            {
                yStart = yFirst + ssFace;
                bodyLength = (spec.depth - ec) - yStart;
                leftEnd = END_SLOT;
                rightEnd = END_HOOK;
                runLabel = "slot " ~ slots[0] ~ " to back wall";
            }
            else
            {
                yStart = yFirst + ssFace;
                bodyLength = (slots[1] * pitch - ssFace) - yStart;
                leftEnd = END_SLOT;
                rightEnd = END_SLOT;
                runLabel = "slot " ~ slots[0] ~ " to slot " ~ slots[1];
            }
            // Room for the wall end's set-back rail, an end rail at the other end,
            // and some web between them.
            if (bodyLength <= fb.hookSetBack + 3 * fb.endRail)
                throw regenError("Front-to-back separator would be too short for its ends.", ["sideSlot"]);

            for (var j = 1; j <= count; j += 1)
            {
                createSeparator(context, id + ("fb" ~ j), mergeMaps(fb, {
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
        "filletEdges" : true,
        "endClearance" : 0.02 * inch,
        "clearance" : 0.0125 * inch,
        "tSlotInterior" : 0.095 * inch,
        "splitForPrinting" : true,
        "jointStyle" : SplitJointStyle.PROUD,
        "maxPrintLength" : 256 * millimeter,
        "jointDepth" : 1 * inch,
        "jointWallThickness" : 0.15 * inch
    });

/**
 * T-slot positions that fall inside one part of a split bar, returned in that
 * part's local u (measured from `origin`).
 */
function localSlots(slotXs is array, origin, lo, hi) returns array
{
    var out = [];
    for (var x in slotXs)
    {
        if (x > lo && x < hi)
            out = append(out, x - origin);
    }
    return out;
}

/**
 * Picks where to split the side-to-side separator, in absolute X.
 *
 * The joint plane, and the joint region that runs `jointDepth` back from it, must stay
 * clear of every T-slot by `margin`, so the joint has to sit inside one of the
 * gaps between neighbouring slots. Returns the feasible point nearest the middle
 * of the bar, which keeps the two printed parts as close to equal as the slot
 * spacing allows.
 */
function splitPosition(slotXs is array, xLow, xHigh, jointDepth, halfSlot, margin) returns map
{
    const centre = (xLow + xHigh) / 2;

    // Gap boundaries: the bar's own ends plus the edges of each slot.
    var edges = [xLow];
    for (var x in slotXs)
    {
        edges = append(edges, x - halfSlot);
        edges = append(edges, x + halfSlot);
    }
    edges = append(edges, xHigh);

    var found = false;
    var best = centre;
    var bestDistance = 0 * meter;
    for (var i = 0; i < size(edges); i += 2)
    {
        // Socket's far end must clear the gap's start; the joint plane its end.
        const low = edges[i] + jointDepth + margin;
        const high = edges[i + 1] - margin;
        if (low > high)
            continue;
        const x = min(max(centre, low), high);
        const distance = abs(x - centre);
        if (!found || distance < bestDistance)
        {
            found = true;
            best = x;
            bestDistance = distance;
        }
    }
    return { "found" : found, "x" : best };
}

/**
 * Builds one separator as a single body, the way it would be machined: a
 * full-thickness block the whole length of the part, cut back to the web
 * everywhere that is not a rail, then finished at each end.
 *
 * Local coordinates: u along the separator (0 at one tip, p.bodyLength at the
 * other), v across the thickness (0 on the mid-plane), z up from the floor.
 *   p.origin / p.uDir / p.vDir / p.zDir : placement
 *   p.leftEnd / p.rightEnd              : END_NONE, END_WALL, END_HOOK, END_SLOT, END_TONGUE or END_GROOVE
 *   p.grooveEnd                         : -1 / +1 to cut a flush-style groove into that end
 *   p.faceSlots                         : u positions of T-slots cut through this separator
 *   p.filletRadius                      : fillet on every edge of the outer faces (0 = none)
 * plus the edge geometry in SS_EDGE or FB_EDGE.
 */
function createSeparator(context is Context, id is Id, p is map)
{
    const zero = 0 * inch;
    const eps = 0.02 * inch;
    const L = p.bodyLength;
    const h = p.height;
    const T = p.thickness;
    const web = p.webThickness;
    const normal = cross(p.uDir, p.zDir);

    localCuboid(context, id + "body", p, zero, L, -T / 2, T / 2, zero, h);
    var solids = [qCreatedBy(id + "body", EntityType.BODY)];

    // Cut both faces back to the web, leaving the top rail and whatever each
    // end keeps. The web runs out through the bottom edge: there is no rail there.
    const pocket = concatenateArrays([pocketChain(p, p.leftEnd, -1, eps), pocketChain(p, p.rightEnd, 1, eps)]);
    var cuts = [];
    for (var side in [-1, 1])
    {
        const tag = side < 0 ? "A" : "B";
        // The slab this face loses, as offsets along the face normal.
        const v0 = side < 0 ? -T / 2 - eps : web / 2;
        const v1 = side < 0 ? -web / 2 : T / 2 + eps;

        localPrism(context, id + ("pocket" ~ tag), p, pocket, v0, v1);
        cuts = append(cuts, qCreatedBy(id + ("pocket" ~ tag), EntityType.BODY));

        for (var dir in [-1, 1])
        {
            const endType = dir < 0 ? p.leftEnd : p.rightEnd;
            const cutId = id + ((dir < 0 ? "left" : "right") ~ tag);
            if (endType == END_WALL)
            {
                // The neck the mount's channel wall slides up.
                neckSlot(context, cutId, p, dir, v0, v1, eps);
                cuts = append(cuts, qCreatedBy(cutId, EntityType.BODY));
            }
            else if (endType == END_HOOK)
            {
                // Everything under the diagonal, out to the tip.
                localPrism(context, cutId, p, fromEnd(p, hookUnderside(p, eps), dir), v0, v1);
                cuts = append(cuts, qCreatedBy(cutId, EntityType.BODY));
            }
        }
    }
    opBoolean(context, id + "pockets", {
        "tools" : qUnion(cuts),
        "targets" : qUnion(solids),
        "operationType" : BooleanOperationType.SUBTRACTION
    });

    // The rib of a front-to-back wall end stands on the web under the diagonal.
    var hooked = false;
    for (var dir in [-1, 1])
    {
        if ((dir < 0 ? p.leftEnd : p.rightEnd) == END_HOOK)
        {
            const ribId = id + (dir < 0 ? "ribLeft" : "ribRight");
            localPrism(context, ribId, p, fromEnd(p, hookRib(p, eps), dir), -p.ribThickness / 2, p.ribThickness / 2);
            solids = append(solids, qCreatedBy(ribId, EntityType.BODY));
            hooked = true;
        }
    }
    if (hooked)
    {
        opBoolean(context, id + "ribs", {
            "tools" : qUnion(solids),
            "operationType" : BooleanOperationType.UNION
        });
    }

    // Bottom corner chamfer on each end that lands in a drawer-wall mount. It
    // starts from the tip, so the head and neck, or the web and rib, are cut
    // back with it. A split joint or a T-slot tongue is an interior junction,
    // so it stays square and the halves meet flush.
    var chamfers = [];
    for (var dir in [-1, 1])
    {
        if (isOuterEnd(dir < 0 ? p.leftEnd : p.rightEnd))
        {
            const chamferId = id + (dir < 0 ? "chamferLeft" : "chamferRight");
            localPrism(context, chamferId, p, fromEnd(p, chamferTriangle(p, 2 * eps), dir), -T / 2 - eps, T / 2 + eps);
            chamfers = append(chamfers, qCreatedBy(chamferId, EntityType.BODY));
        }
    }
    if (size(chamfers) > 0)
    {
        opBoolean(context, id + "chamfers", {
            "tools" : qUnion(chamfers),
            "targets" : qUnion(solids),
            "operationType" : BooleanOperationType.SUBTRACTION
        });
    }

    // Break every edge of the outer faces, and of one face of the rib, as the
    // samples do. Done before the joints and slots go in, which stay sharp.
    if (p.filletRadius > zero)
    {
        const allFaces = qOwnedByBody(qUnion(solids), EntityType.FACE);
        var outerFaces = [
            qCoincidesWithPlane(allFaces, plane(p.origin + (T / 2) * normal, normal)),
            qCoincidesWithPlane(allFaces, plane(p.origin - (T / 2) * normal, -normal))
        ];
        if (hooked)
            outerFaces = append(outerFaces, qCoincidesWithPlane(allFaces, plane(p.origin + (p.ribThickness / 2) * normal, normal)));
        // A fillet that will not build leaves the part sharp-edged rather than
        // failing the whole feature.
        var filleted = false;
        try silent
        {
            opFillet(context, id + "fillet", {
                "entities" : qAdjacent(qUnion(outerFaces), AdjacencyType.EDGE, EntityType.EDGE),
                "radius" : p.filletRadius
            });
            filleted = true;
        }
        if (!filleted)
            reportFeatureInfo(context, p.featureId, "The outer-edge fillet could not be built on " ~ p.name ~ "; its edges are left sharp.");
    }

    // Whatever an interior end carries to meet its neighbour.
    const joints = concatenateArrays([endJoint(context, id + "left", p, p.leftEnd, -1), endJoint(context, id + "right", p, p.rightEnd, 1)]);
    if (size(joints) > 0)
    {
        for (var jointId in joints)
            solids = append(solids, qCreatedBy(jointId, EntityType.BODY));
        opBoolean(context, id + "joints", {
            "tools" : qUnion(solids),
            "operationType" : BooleanOperationType.UNION
        });
    }
    const bodyQuery = qUnion(solids);

    // Flush-style groove: cut back into the end face, full height, open at the
    // top and bottom like any tongue-and-groove edge, and one clearance deeper
    // than the tongue is long so the tongue's shoulders close against the face.
    if (p.grooveEnd != 0)
    {
        const dirG = p.grooveEnd;
        const baseG = dirG < 0 ? zero : L;
        localCuboid(context, id + "grooveCut", p,
            baseG - dirG * (p.jointDepth + p.clearance), baseG + dirG * eps,
            -p.jointGrooveHalf, p.jointGrooveHalf, -eps, h + eps);
        opBoolean(context, id + "groove", {
            "tools" : qCreatedBy(id + "grooveCut", EntityType.BODY),
            "targets" : bodyQuery,
            "operationType" : BooleanOperationType.SUBTRACTION
        });
    }

    // Vertical T-slots through the full thickness for mating separators,
    // open at the top and closed at the slot floor.
    if (size(p.faceSlots) > 0)
    {
        var slotCuts = [];
        for (var i = 0; i < size(p.faceSlots); i += 1)
        {
            const u = p.faceSlots[i];
            const cutId = id + ("slot" ~ i);
            localCuboid(context, cutId, p, u - p.faceSlotWidth / 2, u + p.faceSlotWidth / 2, -T / 2 - eps, T / 2 + eps, p.slotFloor, h + eps);
            slotCuts = append(slotCuts, qCreatedBy(cutId, EntityType.BODY));
        }
        opBoolean(context, id + "slots", {
            "tools" : qUnion(slotCuts),
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
 * Whether an end of a separator is an outer end of the assembled divider — one
 * that lands in a drawer-wall mount — rather than an interior junction with
 * another separator or with the other half of a split bar.
 */
function isOuterEnd(endType is string) returns boolean
{
    return endType == END_WALL || endType == END_HOOK;
}

/**
 * How far in from an end the full-thickness material runs before the web
 * pocket starts. Not used for END_HOOK, whose pocket follows the diagonal.
 */
function endInset(p is map, endType is string, dir is number)
{
    if (endType == END_WALL)
        return p.wallHead + p.wallNeck + p.endRail;
    // A groove cut into the end needs a solid collar around it, then the rail.
    if (p.grooveEnd == dir)
        return p.jointDepth + p.clearance + p.endRail;
    return p.endRail;
}

/**
 * One end's side of the web pocket outline, as [u, z] points. The left end's
 * run from the underside of the top rail down through the bottom edge and the
 * right end's back up, so the two together close the outline. `margin` is how
 * far the outline overshoots the bottom edge.
 */
function pocketChain(p is map, endType is string, dir is number, margin) returns array
{
    const zRail = p.height - p.topRail;
    var chain;
    if (endType == END_HOOK)
    {
        // Down the end rail, along the inside of the diagonal, then down the
        // set-back rail.
        const eNear = p.endRail;
        const eFar = p.hookSetBack + p.endRail;
        chain = [[eNear, zRail], [eNear, hookInner(p, eNear)], [eFar, hookInner(p, eFar)], [eFar, -margin]];
    }
    else
    {
        const inset = endInset(p, endType, dir);
        chain = [[inset, zRail], [inset, -margin]];
    }
    if (dir < 0)
        return chain;
    var mirrored = [];
    for (var i = size(chain) - 1; i >= 0; i -= 1)
        mirrored = append(mirrored, [p.bodyLength - chain[i][0], chain[i][1]]);
    return mirrored;
}

/**
 * Converts [e, z] points, with e measured in from the tip of an end, to the
 * separator's own [u, z].
 */
function fromEnd(p is map, points is array, dir is number) returns array
{
    if (dir < 0)
        return points;
    var out = [];
    for (var pt in points)
        out = append(out, [p.bodyLength - pt[0], pt[1]]);
    return out;
}

/**
 * Height of the outside of a front-to-back wall end's diagonal, e in from the
 * tip: the line the end rail's tip face turns into, which is also the top edge
 * of the web and rib beneath it.
 */
function hookOuter(p is map, e)
{
    return p.height - p.hookUpperDrop - (p.hookDiagonalDrop / p.hookSetBack) * e;
}

/** Height of the inside of the diagonal, one rail width square off the outside. */
function hookInner(p is map, e)
{
    const slope = p.hookDiagonalDrop / p.hookSetBack;
    return hookOuter(p, e) + p.endRail * sqrt(1 + slope * slope);
}

/**
 * Outline of everything under a front-to-back wall end's diagonal, from past
 * the tip to the set-back rail, as [e, z] points measured in from the tip.
 */
function hookUnderside(p is map, margin) returns array
{
    return [
        [-margin, hookOuter(p, -margin)],
        [p.hookSetBack, hookOuter(p, p.hookSetBack)],
        [p.hookSetBack, -margin],
        [-margin, -margin]
    ];
}

/**
 * Outline of a front-to-back wall end's rib, as [e, z] points measured in from
 * the tip. Its top is the underside of the diagonal, overlapped into the rail
 * so the two fuse; its far face tapers toward the tip on the way down; and it
 * overshoots the bottom edge by `margin`, for the chamfer to trim.
 */
function hookRib(p is map, margin) returns array
{
    const overlap = 0.02 * inch;
    const zTop = hookOuter(p, p.ribEndTop);
    const zBottom = p.chamferRise * (1 - p.ribEndBottom / p.chamferRun);
    // The far face runs from the diagonal to the chamfer; carry it on down.
    const eLow = p.ribEndBottom - (p.ribEndTop - p.ribEndBottom) * (zBottom + margin) / (zTop - zBottom);
    return [
        [p.ribStart, hookOuter(p, p.ribStart) + overlap],
        [p.ribEndTop, zTop + overlap],
        [p.ribEndTop, zTop],
        [eLow, -margin],
        [p.ribStart, -margin]
    ];
}

/**
 * Outline of the bottom corner chamfer at a wall end, as [e, z] points measured
 * in from the tip. The sloping side runs from p.chamferRise up the tip to
 * p.chamferRun along the bottom edge; the other two overshoot the part by `margin`.
 */
function chamferTriangle(p is map, margin) returns array
{
    return [
        [-margin, -margin],
        [p.chamferRun * (1 + margin / p.chamferRise), -margin],
        [-margin, p.chamferRise * (1 + margin / p.chamferRun)]
    ];
}

/**
 * Prism with the given [u, z] outline, in the separator's local coordinates,
 * running from v0 to v1 along the normal of the separator's faces.
 */
function localPrism(context is Context, id is Id, p is map, points is array, v0, v1)
{
    const normal = cross(p.uDir, p.zDir);
    const sketch = newSketchOnPlane(context, id + "sketch", {
        "sketchPlane" : plane(p.origin + v0 * normal, normal, p.uDir)
    });
    var outline = [];
    for (var pt in points)
        outline = append(outline, vector(pt[0], pt[1]));
    outline = append(outline, outline[0]);
    skPolyline(sketch, "outline", { "points" : outline });
    skSolve(sketch);
    opExtrude(context, id, {
        "entities" : qSketchRegion(id + "sketch"),
        "direction" : normal,
        "endBound" : BoundingType.BLIND,
        "endDepth" : v1 - v0
    });
    opDeleteBodies(context, id + "deleteSketch", {
        "entities" : qCreatedBy(id + "sketch", EntityType.BODY)
    });
}

/**
 * Tool for one face of a side-to-side wall end's neck: a slot between the head
 * and the end rail, running from v0 to v1 along the face normal. It is open
 * through the bottom edge (by `margin`) and closed under the top rail by a full
 * round, whose straight sides stop level with the rail's underside.
 */
function neckSlot(context is Context, id is Id, p is map, dir is number, v0, v1, margin)
{
    const normal = cross(p.uDir, p.zDir);
    const base = dir < 0 ? 0 * inch : p.bodyLength;
    const uHead = base - dir * p.wallHead;
    const uRail = base - dir * (p.wallHead + p.wallNeck);
    const zRail = p.height - p.topRail;
    const sketch = newSketchOnPlane(context, id + "sketch", {
        "sketchPlane" : plane(p.origin + v0 * normal, normal, p.uDir)
    });
    skLineSegment(sketch, "headSide", { "start" : vector(uHead, -margin), "end" : vector(uHead, zRail) });
    skArc(sketch, "round", {
        "start" : vector(uHead, zRail),
        "mid" : vector((uHead + uRail) / 2, zRail + p.wallNeck / 2),
        "end" : vector(uRail, zRail)
    });
    skLineSegment(sketch, "railSide", { "start" : vector(uRail, zRail), "end" : vector(uRail, -margin) });
    skLineSegment(sketch, "bottom", { "start" : vector(uRail, -margin), "end" : vector(uHead, -margin) });
    skSolve(sketch);
    opExtrude(context, id, {
        "entities" : qSketchRegion(id + "sketch"),
        "direction" : normal,
        "endBound" : BoundingType.BLIND,
        "endDepth" : v1 - v0
    });
    opDeleteBodies(context, id + "deleteSketch", {
        "entities" : qCreatedBy(id + "sketch", EntityType.BODY)
    });
}

/**
 * Adds what an interior end carries to meet its neighbour.
 * dir = -1 for the u = 0 end, +1 for the u = bodyLength end.
 * Returns the ids of the pieces created so the caller can union them. The wall
 * ends, END_WALL and END_HOOK, are cut from the separator itself and add nothing.
 *
 * END_SLOT           : half-depth tongue into another separator's T-slot.
 * END_TONGUE         : male half of the split joint, a full-height tongue that
 *                      slides into the groove in the mating side-to-side part.
 * END_GROOVE         : female half in the PROUD style, a raised sleeve of two
 *                      walls standing out from the bar's faces; the groove is
 *                      the gap between them. The FLUSH style instead cuts the
 *                      groove into the end face, via p.grooveEnd.
 */
function endJoint(context is Context, id is Id, p is map, endType is string, dir is number) returns array
{
    const zero = 0 * inch;
    const overlap = 0.02 * inch;
    const base = dir < 0 ? zero : p.bodyLength;

    if (endType == END_TONGUE)
    {
        // Male half of the split joint: a tongue the thickness of the web running
        // the full height of the end face, so it continues the web of the part it
        // slides into. In the PROUD style its tip butts the mating part's end
        // face, a solid full-section seat; in FLUSH it enters the cut groove and
        // its own shoulders close against that part's end face.
        localCuboid(context, id + "tongue", p, base - dir * overlap, base + dir * p.jointDepth,
            -p.jointTongueThickness / 2, p.jointTongueThickness / 2, zero, p.height);
        return [id + "tongue"];
    }

    if (endType == END_GROOVE)
    {
        // Female half of the split joint in the PROUD style. Rather than carving
        // a groove back into the bar and leaving a thin skin either side, two
        // walls are added off the end face and the groove is simply the gap
        // between them. Their outer faces stand out from the bar's faces, so the
        // bar keeps its full section right up to the seat and the walls are as
        // thick as they need to be. They stop a clearance short of the mating
        // body, so the tongue's tip seats on this part's end face instead.
        const reach = p.jointDepth - p.clearance;
        localCuboid(context, id + "wallA", p, base - dir * overlap, base + dir * reach,
            p.jointGrooveHalf, p.jointSleeveHalf, zero, p.height);
        localCuboid(context, id + "wallB", p, base - dir * overlap, base + dir * reach,
            -p.jointSleeveHalf, -p.jointGrooveHalf, zero, p.height);
        return [id + "wallA", id + "wallB"];
    }

    if (endType == END_SLOT)
    {
        // Tongue into another separator's T-slot. It reaches only that separator's
        // mid-plane instead of passing all the way through with a retaining head,
        // so the front-to-back run in the facing cell can enter the same slot from
        // the other side; the two tongues meet in the slot with a clearance gap.
        // No head is needed: each run is captive lengthwise between its wall mount
        // (or its other slot) and the separator it plugs into.
        // It also stops above the slot floor so it seats on solid material.
        localCuboid(context, id + "tongue", p, base - dir * overlap, base + dir * p.slotEngageDepth,
            -p.slotNeckThickness / 2, p.slotNeckThickness / 2, p.slotFloor + p.clearance, p.height);
        return [id + "tongue"];
    }

    return [];
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
