FeatureScript 3070;
import(path : "onshape/std/common.fs", version : "3070.0");

/*
 * StackTech Separators
 *
 * Builds 3D-printable divider bars for ToughBuilt StackTech drawers:
 *   - a full-width SIDE-TO-SIDE separator that drops into the female
 *     receivers on the drawer side walls, and
 *   - FRONT-TO-BACK separators that tongue into the front or back wall and
 *     hook over the side-to-side separator with a half-lap (egg-crate) joint.
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
 *   sideSlots  : female receivers on each side wall, counted front to back
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

const END_TONGUE = "TONGUE";
const END_NOTCH = "NOTCH";
const END_PLAIN = "PLAIN";

const SLOT_BOUNDS = { (unitless) : [1, 1, 8] } as IntegerBoundSpec;
const SECOND_SLOT_BOUNDS = { (unitless) : [1, 2, 8] } as IntegerBoundSpec;
const COUNT_BOUNDS = { (unitless) : [1, 1, 8] } as IntegerBoundSpec;
const POSITION_BOUNDS = { (unitless) : [1, 2, 8] } as IntegerBoundSpec;

const THICKNESS_BOUNDS = { (meter) : [0.0005, 0.0024, 0.01], (millimeter) : 2.4, (centimeter) : 0.24, (inch) : 0.0945, (foot) : 0.00787, (yard) : 0.00262 } as LengthBoundSpec;
const END_CLEARANCE_BOUNDS = { (meter) : [0, 0.0005, 0.02], (millimeter) : 0.5, (centimeter) : 0.05, (inch) : 0.02, (foot) : 0.00164, (yard) : 0.00055 } as LengthBoundSpec;
const TOP_CLEARANCE_BOUNDS = { (meter) : [0, 0.004, 0.1], (millimeter) : 4, (centimeter) : 0.4, (inch) : 0.157, (foot) : 0.0131, (yard) : 0.00437 } as LengthBoundSpec;
const TONGUE_DEPTH_BOUNDS = { (meter) : [0, 0.003, 0.03], (millimeter) : 3, (centimeter) : 0.3, (inch) : 0.118, (foot) : 0.00984, (yard) : 0.00328 } as LengthBoundSpec;
const TONGUE_HEIGHT_BOUNDS = { (meter) : [0, 0, 0.5], (millimeter) : 0, (centimeter) : 0, (inch) : 0, (foot) : 0, (yard) : 0 } as LengthBoundSpec;
const JOINT_CLEARANCE_BOUNDS = { (meter) : [0, 0.0003, 0.005], (millimeter) : 0.3, (centimeter) : 0.03, (inch) : 0.0118, (foot) : 0.00098, (yard) : 0.00033 } as LengthBoundSpec;

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

            annotation { "Name" : "Clearance below the drawer rim" }
            isLength(definition.topClearance, TOP_CLEARANCE_BOUNDS);

            annotation { "Name" : "Clearance at each wall" }
            isLength(definition.endClearance, END_CLEARANCE_BOUNDS);

            annotation { "Name" : "Wall tab depth (0 = none)" }
            isLength(definition.tongueDepth, TONGUE_DEPTH_BOUNDS);

            annotation { "Name" : "Wall tab height from the top (0 = full height)" }
            isLength(definition.tongueHeight, TONGUE_HEIGHT_BOUNDS);

            annotation { "Name" : "Joint clearance" }
            isLength(definition.jointClearance, JOINT_CLEARANCE_BOUNDS);
        }
    }
    {
        const spec = drawerSpec(definition.drawer);
        const zero = 0 * millimeter;

        if (definition.sideSlot > spec.sideSlots)
            throw regenError("The " ~ spec.label ~ " has only " ~ spec.sideSlots ~ " receivers per side wall.", ["sideSlot"]);

        const wantSS = definition.kind != SeparatorKind.FRONT_TO_BACK;
        const wantFB = definition.kind != SeparatorKind.SIDE_TO_SIDE;
        const between = wantFB && definition.anchor == FrontToBackAnchor.BETWEEN_SEPARATORS;

        var slots = [definition.sideSlot];
        if (between)
        {
            if (definition.secondSideSlot <= definition.sideSlot)
                throw regenError("The second slot must be further back than the first slot.", ["secondSideSlot"]);
            if (definition.secondSideSlot > spec.sideSlots)
                throw regenError("The " ~ spec.label ~ " has only " ~ spec.sideSlots ~ " receivers per side wall.", ["secondSideSlot"]);
            slots = append(slots, definition.secondSideSlot);
        }

        const positions = definition.overridePositions ? definition.positions : spec.positions;
        const t = definition.thickness;
        const h = spec.height - definition.topClearance;
        if (h <= t)
            throw regenError("Rim clearance leaves no separator height.", ["topClearance"]);

        const pitch = spec.depth / (spec.sideSlots + 1);
        const colPitch = spec.width / (positions + 1);
        const ec = definition.endClearance;
        const notchWidth = t + definition.jointClearance;
        const notchDepth = h / 2;
        const tongueZ1 = h;
        const tongueZ0 = definition.tongueHeight <= zero ? zero : max(zero, h - definition.tongueHeight);
        const wallEnd = definition.tongueDepth > zero ? END_TONGUE : END_PLAIN;

        const common = {
            "height" : h,
            "thickness" : t,
            "tongueDepth" : definition.tongueDepth,
            "tongueZ0" : tongueZ0,
            "tongueZ1" : tongueZ1,
            "topNotches" : [],
            "topNotchDepth" : notchDepth,
            "bottomNotchWidth" : notchWidth,
            "bottomNotchDepth" : notchDepth
        };

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
            var topNotches = [];
            for (var j = 1; j <= positions; j += 1)
            {
                const u = j * colPitch - ec;
                topNotches = append(topNotches, [u - notchWidth / 2, u + notchWidth / 2]);
            }
            for (var k in slots)
            {
                const yc = k * pitch;
                createSeparator(context, id + ("ss" ~ k), mergeMaps(common, {
                    "origin" : vector(ec, yc + t / 2, zero),
                    "xDir" : vector(1, 0, 0),
                    "normal" : vector(0, -1, 0),
                    "length" : spec.width - 2 * ec,
                    "leftEnd" : wallEnd,
                    "rightEnd" : wallEnd,
                    "topNotches" : topNotches,
                    "name" : "StackTech side-to-side separator - " ~ spec.label ~ " - slot " ~ k
                }));
            }
        }

        if (wantFB)
        {
            const count = min(definition.frontToBackCount, positions);
            const yFirst = slots[0] * pitch;
            var yStart;
            var length;
            var leftEnd;
            var rightEnd;
            var runLabel;
            if (definition.anchor == FrontToBackAnchor.FROM_FRONT_WALL)
            {
                yStart = ec;
                length = (yFirst + notchWidth / 2) - yStart;
                leftEnd = wallEnd;
                rightEnd = END_NOTCH;
                runLabel = "front wall to slot " ~ slots[0];
            }
            else if (definition.anchor == FrontToBackAnchor.FROM_BACK_WALL)
            {
                yStart = yFirst - notchWidth / 2;
                length = (spec.depth - ec) - yStart;
                leftEnd = END_NOTCH;
                rightEnd = wallEnd;
                runLabel = "slot " ~ slots[0] ~ " to back wall";
            }
            else
            {
                yStart = yFirst - notchWidth / 2;
                length = (slots[1] * pitch + notchWidth / 2) - yStart;
                leftEnd = END_NOTCH;
                rightEnd = END_NOTCH;
                runLabel = "slot " ~ slots[0] ~ " to slot " ~ slots[1];
            }
            if (length <= 3 * notchWidth)
                throw regenError("Front-to-back separator would be too short for its joints.", ["sideSlot"]);

            for (var j = 1; j <= count; j += 1)
            {
                const xc = j * colPitch;
                createSeparator(context, id + ("fb" ~ j), mergeMaps(common, {
                    "origin" : vector(xc - t / 2, yStart, zero),
                    "xDir" : vector(0, 1, 0),
                    "normal" : vector(1, 0, 0),
                    "length" : length,
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
        "thickness" : 2.4 * millimeter,
        "topClearance" : 4 * millimeter,
        "endClearance" : 0.5 * millimeter,
        "tongueDepth" : 3 * millimeter,
        "tongueHeight" : 0 * millimeter,
        "jointClearance" : 0.3 * millimeter
    });

/**
 * Sketches the side profile of one separator on the plane described by
 * p.origin / p.normal / p.xDir, extrudes it by p.thickness and names the body.
 * Profile coordinates: u along the separator (0 at the plate start), v up.
 */
function createSeparator(context is Context, id is Id, p is map)
{
    const pts = separatorProfile(p);
    if (size(pts) < 5)
        throw regenError("Separator profile is degenerate.");

    const sketch = newSketchOnPlane(context, id + "sketch", {
        "sketchPlane" : plane(p.origin, p.normal, p.xDir)
    });
    skPolyline(sketch, "profile", { "points" : pts });
    skSolve(sketch);

    opExtrude(context, id + "extrude", {
        "entities" : qSketchRegion(id + "sketch"),
        "direction" : p.normal,
        "endBound" : BoundingType.BLIND,
        "endDepth" : p.thickness
    });
    opDeleteBodies(context, id + "deleteSketch", {
        "entities" : qCreatedBy(id + "sketch", EntityType.BODY)
    });
    setProperty(context, {
        "entities" : qCreatedBy(id + "extrude", EntityType.BODY),
        "propertyType" : PropertyType.NAME,
        "value" : p.name
    });
}

/**
 * Closed polyline (counter-clockwise) for a separator: a length x height
 * plate with optional wall tongues sticking out of either end, half-lap
 * notches cut up from the bottom at either end, and half-lap notches cut
 * down from the top edge at p.topNotches ([u0, u1] pairs, ascending).
 */
function separatorProfile(p is map) returns array
{
    const zero = 0 * millimeter;
    const L = p.length;
    const h = p.height;
    const nw = p.bottomNotchWidth;
    const nd = p.bottomNotchDepth;
    const td = p.tongueDepth;
    const leftTongue = p.leftEnd == END_TONGUE && td > zero;
    const rightTongue = p.rightEnd == END_TONGUE && td > zero;
    const leftNotch = p.leftEnd == END_NOTCH;
    const rightNotch = p.rightEnd == END_NOTCH;

    var pts = [];
    const startV = leftNotch ? nd : zero;
    pts = addPoint(pts, vector(zero, startV));
    if (leftNotch)
    {
        pts = addPoint(pts, vector(nw, nd));
        pts = addPoint(pts, vector(nw, zero));
    }
    if (rightNotch)
    {
        pts = addPoint(pts, vector(L - nw, zero));
        pts = addPoint(pts, vector(L - nw, nd));
        pts = addPoint(pts, vector(L, nd));
    }
    else
    {
        pts = addPoint(pts, vector(L, zero));
    }
    if (rightTongue)
    {
        pts = addPoint(pts, vector(L, p.tongueZ0));
        pts = addPoint(pts, vector(L + td, p.tongueZ0));
        pts = addPoint(pts, vector(L + td, p.tongueZ1));
        pts = addPoint(pts, vector(L, p.tongueZ1));
    }
    pts = addPoint(pts, vector(L, h));
    for (var i = size(p.topNotches) - 1; i >= 0; i -= 1)
    {
        const u0 = p.topNotches[i][0];
        const u1 = p.topNotches[i][1];
        pts = addPoint(pts, vector(u1, h));
        pts = addPoint(pts, vector(u1, h - p.topNotchDepth));
        pts = addPoint(pts, vector(u0, h - p.topNotchDepth));
        pts = addPoint(pts, vector(u0, h));
    }
    pts = addPoint(pts, vector(zero, h));
    if (leftTongue)
    {
        pts = addPoint(pts, vector(zero, p.tongueZ1));
        pts = addPoint(pts, vector(-td, p.tongueZ1));
        pts = addPoint(pts, vector(-td, p.tongueZ0));
        pts = addPoint(pts, vector(zero, p.tongueZ0));
    }
    pts = addPoint(pts, pts[0]);
    return pts;
}

function addPoint(pts is array, p is Vector) returns array
{
    if (size(pts) > 0 && tolerantEquals(pts[size(pts) - 1], p))
        return pts;
    return append(pts, p);
}
