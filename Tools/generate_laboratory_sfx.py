"""Build light mechanical Foley; the two skills add a quiet electronic field layer.

python Tools/generate_laboratory_sfx.py (Python standard library only).
Source/CC0/SOURCES.md retains authors, licenses and recording origins.
Source/Decoded holds PCM copies of the retained original Kenney OGG files.
"""
from __future__ import annotations
import array
import argparse
import json
import math
from pathlib import Path
import random
import sys
import wave

RATE = 44100
ROOT = Path(__file__).resolve().parents[1]
BASE = ROOT / "Assets/Whitebox/ArtAssets/Audio"
OUTPUT = BASE / "SFX"
IMPACTS = BASE / "Source/Decoded/Kenney_ImpactSounds"
SCRAPES = BASE / "Source/CC0/AntumDeluge_Scrapes"
_cache: dict[str, list[float]] = {}


def silence(seconds):
    return [0.0] * round(seconds * RATE)


def resample(values, speed):
    result = []
    for i in range(max(2, round(len(values) / speed))):
        at = min(len(values) - 1, i * speed)
        left, fraction = int(at), at - int(at)
        result.append(values[left] * (1 - fraction) + values[min(left + 1, len(values) - 1)] * fraction)
    return result


def read(name):
    if name in _cache:
        return list(_cache[name])
    path = (SCRAPES if name.startswith("scrape-") else IMPACTS) / (name + ".wav")
    with wave.open(str(path), "rb") as audio:
        assert audio.getsampwidth() == 2, path
        channels, rate = audio.getnchannels(), audio.getframerate()
        pcm = array.array("h", audio.readframes(audio.getnframes()))
    if sys.byteorder != "little":
        pcm.byteswap()
    values = [sum(pcm[i:i + channels]) / (32768.0 * channels) for i in range(0, len(pcm), channels)]
    if rate != RATE:
        values = resample(values, rate / RATE)
    peak = max(abs(x) for x in values)
    active = [i for i, x in enumerate(values) if abs(x) > peak * .015]
    values = values[max(0, active[0] - 220):min(len(values), active[-1] + 882)]
    center = sum(values) / len(values)
    values = [x - center for x in values]
    _cache[name] = values
    return list(values)


def biquad(values, cutoff, highpass=False):
    # Filter recorded material; this does not generate an oscillator or a new tone.
    omega = 2 * math.pi * cutoff / RATE
    cosine, alpha = math.cos(omega), math.sin(omega) / math.sqrt(2)
    a0 = 1 + alpha
    b0 = ((1 + cosine) if highpass else (1 - cosine)) / (2 * a0)
    b1 = (-(1 + cosine) if highpass else (1 - cosine)) / a0
    b2, a1, a2 = b0, -2 * cosine / a0, (1 - alpha) / a0
    x1 = x2 = y1 = y2 = 0.0
    result = []
    for x in values:
        y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2
        result.append(y)
        x2, x1, y2, y1 = x1, x, y1, y
    return result


def soften(values, cutoff=2100):
    return biquad(biquad(biquad(values, 65, highpass=True), cutoff), cutoff)


def edges(values, attack=.008, release=.045):
    result = list(values)
    n = min(len(result), round(attack * RATE))
    for i in range(n):
        result[i] *= .5 - .5 * math.cos(math.pi * i / max(1, n))
    n = min(len(result), round(release * RATE))
    for i in range(n):
        result[-1 - i] *= .5 - .5 * math.cos(math.pi * i / max(1, n))
    return result


def material(name, speed=1, cutoff=2100, length=None, start=0):
    values = read(name)
    first = round(start * RATE)
    values = values[first:first + round(length * RATE)] if length is not None else values[first:]
    values = soften(resample(values, speed), cutoff)
    peak = max(abs(x) for x in values)
    return edges([x * .8 / max(peak, .0001) for x in values])


def mix(destination, source, start, gain):
    first = round(start * RATE)
    for i in range(min(len(source), len(destination) - first)):
        destination[first + i] += source[i] * gain


def friction(duration, seed, cutoff=1700):
    """Irregular real dragging texture; long overlaps avoid repeated pulses."""
    rng, overlap, values = random.Random(seed), round(.14 * RATE), []
    while len(values) < round((duration + .4) * RATE):
        raw = read(f"scrape-{rng.choice((1, 1, 2, 2, 3, 4, 5, 6, 7, 8))}")
        count = min(len(raw), round(rng.uniform(.65, .95) * RATE))
        first = rng.randrange(max(1, len(raw) - count + 1))
        chunk = soften(resample(raw[first:first + count], rng.uniform(.86, 1.03)), cutoff)
        rms = math.sqrt(sum(x * x for x in chunk) / len(chunk))
        chunk = [x * min(3.0, .10 / max(.01, rms)) for x in chunk]
        if not values:
            values = chunk
            continue
        n = min(overlap, len(values), len(chunk))
        for i in range(n):
            phase = i / max(1, n - 1) * math.pi * .5
            values[-n + i] = values[-n + i] * math.cos(phase) + chunk[i] * math.sin(phase)
        values.extend(chunk[n:])
    n = round(.18 * RATE)
    values = values[:round(duration * RATE) + n]
    head = [values[-n + i] * math.cos(i / (n - 1) * math.pi * .5) +
            values[i] * math.sin(i / (n - 1) * math.pi * .5) for i in range(n)]
    return head + values[n:-n]


def footstep(variant):
    out, index = silence(.17 + (variant % 3) * .008), (variant - 1) % 3
    # A brief sole contact carries the step. Equipment is a detail, not a heavy chassis.
    sole = material(f"footstep_concrete_{index:03d}", 1.10 + .012 * variant, 2300)
    sole = edges(biquad(biquad(sole, 220, True), 220, True), .003, .025)
    mix(out, sole, 0, .54)
    joint = material(f"impactPlate_light_{index:03d}", 1.35, 2100, .065)
    mix(out, biquad(joint, 280, True), .017, .040)
    scrape = material(f"scrape-{1 + index}", 1.1, 1900, .070, .14 + .035 * (variant // 3))
    mix(out, biquad(scrape, 280, True), .030, .045)
    return out


def jump():
    out = silence(.21)
    # Brief release of ground pressure, with only a trace of surface/equipment noise.
    push = material("impactSoft_heavy_000", 1.4, 1900, .13)
    push = edges(biquad(biquad(push, 220, True), 220, True), .004, .025)
    mix(out, push, 0, .55)
    scrape = material("scrape-2", 1.25, 1700, .065, .15)
    mix(out, biquad(scrape, 280, True), .018, .035)
    joint = material("impactMetal_medium_001", 1.35, 1750, .055)
    mix(out, biquad(joint, 280, True), .012, .035)
    return out


def field_pulse(duration, frequency, bend=0):
    """Rounded, low-mid electronic energy; slight motion, no piercing sweep/beep."""
    out, phase = [], 0.0
    for i in range(round(duration * RATE)):
        t = i / RATE
        phase += 2 * math.pi * (frequency + bend * t / duration) / RATE
        color = phase + .24 * math.sin(2 * math.pi * 23 * t)
        out.append(math.sin(color) * .82 + math.sin(color * 2) * .13)
    return edges(out, .035, duration * .65)


def add_field(mechanical, pulses, relative_db):
    layer = silence(len(mechanical) / RATE)
    for at, duration, frequency, bend, gain in pulses:
        mix(layer, field_pulse(duration, frequency, bend), at, gain)
    rms = lambda values: math.sqrt(sum(x * x for x in values) / len(values))
    gain = rms(mechanical) * 10 ** (relative_db / 20) / max(.0001, rms(layer))
    return [body + field * gain for body, field in zip(mechanical, layer)]


def skill_velocity(electronic=True):
    out = silence(.52)
    mix(out, material("impactTin_medium_001", .8, 1450), 0, .32)
    mix(out, material("scrape-1", 1.05, 1550, .32, .12), .03, .70)
    mix(out, material("impactSoft_heavy_000", 1.18, 1200, .24), .09, .36)
    mix(out, material("impactMetal_medium_002", .86, 1150), .24, .17)
    return add_field(out, ((.055, .22, 410, 24, 1),), -16) if electronic else out


def skill_gravity(electronic=True):
    out = silence(.73)
    mix(out, material("impactPlate_heavy_001", .8, 1250, .28), 0, .45)
    mix(out, material("scrape-2", .72, 1400, .38, .16), .055, .53)
    mix(out, material("impactSoft_heavy_002", .83, 1000, .3), .24, .52)
    mix(out, material("impactMetal_heavy_002", .8, 1200), .43, .27)
    pulses = ((.075, .29, 245, -16, 1), (.34, .24, 245, -12, .65))
    return add_field(out, pulses, -15) if electronic else out


def player_death():
    out = silence(1.2)
    mix(out, material("impactSoft_heavy_001", .8, 1400), 0, .70)
    for i, (at, gain) in enumerate(((.01, .54), (.13, .42), (.29, .34), (.47, .24), (.68, .17), (.87, .09))):
        name = ("impactMetal_heavy", "impactPlate_light", "impactTin_medium")[i % 3]
        mix(out, material(f"{name}_{i % 3:03d}", .83 + .055 * i, 1900), at, gain)
    mix(out, material("scrape-4", .9, 1500, .3, .18), .34, .14)
    return out


def turret_fire():
    out = silence(.33)
    mix(out, material("impactSoft_heavy_002", 1.2, 1600, .23), 0, .66)
    mix(out, material("impactMetal_heavy_000", 1.04, 1900), .01, .39)
    mix(out, material("scrape-1", 1.3, 2000, .18, .4), .055, .15)
    return out


def bullet_hit(player):
    out = silence(.36 if player else .31)
    mix(out, material("impactSoft_heavy_000" if player else "impactMetal_heavy_002", .98 if player else 1.05, 1500 if player else 2200, .27), 0, .72)
    mix(out, material("impactPlate_heavy_000" if player else "impactTin_medium_000", .9, 1500 if player else 2100, .24), .008, .27)
    return out


def latch(kind):
    out = silence(.4 if kind == "checkpoint" else .28)
    if kind == "plate":
        mix(out, material("impactPlate_heavy_001", .85, 1500, .22), 0, .55)
        mix(out, material("impactSoft_heavy_000", 1.05, 1100, .17), .018, .3)
    else:
        mix(out, material("impactTin_medium_001", .86 if kind == "checkpoint" else 1, 1700), 0, .5)
        mix(out, material("impactMetal_medium_002", .88, 1550), .13 if kind == "checkpoint" else .075, .32)
    return out


def door(opening):
    out = silence(1.02 if opening else .92)
    mix(out, material("impactMetal_medium_001", .85, 1400), 0, .25)
    mix(out, edges(friction(.72, 70 + int(opening), 1550), .05, .09), .06, .9)
    mix(out, material("impactPlate_heavy_000" if opening else "impactMetal_heavy_002", .85, 1450), .77 if opening else .69, .38 if opening else .50)
    return out


def portal():
    out = silence(1.4)
    mix(out, edges(friction(1.15, 810, 1350), .22, .3), .08, 1.2)
    mix(out, material("impactSoft_heavy_001", .75, 1000), .3, .52)
    mix(out, material("impactMetal_medium_000", .8, 1300, .18), .015, .22)
    return out


def write_clip(name, values, peak, looping=False, drive=1.35):
    assert values and all(math.isfinite(x) for x in values), name
    if not looping:
        values = edges(values, .008, .04)
    center = sum(values) / len(values)
    values = [x - center for x in values]
    if not looping:
        values = edges(values, .003, .01)
    maximum = max(abs(x) for x in values)
    assert maximum > .0001, name
    if drive:
        values = [math.tanh(x / maximum * drive) for x in values]
    maximum = max(abs(x) for x in values)
    pcm = array.array("h", (round(x / maximum * peak * 32767) for x in values))
    if sys.byteorder != "little":
        pcm.byteswap()
    with wave.open(str(OUTPUT / (name + ".wav")), "wb") as out:
        out.setnchannels(1)
        out.setsampwidth(2)
        out.setframerate(RATE)
        out.writeframes(pcm.tobytes())
    samples = [x / 32767 for x in pcm]
    rms = math.sqrt(sum(x * x for x in samples) / len(samples))
    seam = abs(samples[-1] - samples[0])
    assert rms > .001 and peak <= .65, name
    if looping:
        assert seam < .012, (name, seam)
    else:
        assert samples[0] == samples[-1] == 0, name
    return {"seconds": len(samples) / RATE, "peak": peak, "rms": rms, "seam": seam}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--only", nargs="+", help="Cue names; Footstep selects all six variants")
    args = parser.parse_args()
    OUTPUT.mkdir(parents=True, exist_ok=True)
    clips = [(f"Footstep_{i:02d}", footstep(i), .26, False) for i in range(1, 7)]
    clips += [
        ("Jump", jump(), .31, False), ("SkillVelocity", skill_velocity(), .48, False),
        ("SkillGravity", skill_gravity(), .50, False), ("PlayerDeath", player_death(), .62, False),
        ("TurretAim", friction(4.6, 602, 1000), .24, True), ("TurretFire", turret_fire(), .57, False),
        ("BulletHitPlayer", bullet_hit(True), .58, False), ("BulletHitWall", bullet_hit(False), .55, False),
        ("Checkpoint", latch("checkpoint"), .46, False), ("PressurePlate", latch("plate"), .48, False),
        ("BulletSwitch", latch("switch"), .47, False), ("DoorOpen", door(True), .48, False),
        ("DoorClose", door(False), .50, False), ("Portal", portal(), .54, False),
        ("CrateScrape", friction(5.8, 900, 1900), .45, True),
    ]
    if args.only:
        available = {name for name, *_ in clips} | {"Footstep"}
        unknown = set(args.only) - available
        if unknown:
            parser.error("Unknown cue: " + ", ".join(sorted(unknown)))
        clips = [clip for clip in clips if clip[0] in args.only or
                 ("Footstep" in args.only and clip[0].startswith("Footstep_"))]
    report_path = OUTPUT / "FoleyProcessing.json"
    report = json.loads(report_path.read_text(encoding="utf-8")) if args.only and report_path.exists() else {}
    print("CC0 recorded Foley + subtle skill electronics | 44.1 kHz mono PCM16")
    for name, values, peak, loop in clips:
        # Preserve short movement transients instead of inflating their rubbing tails.
        stats = write_clip(name, values, peak, loop, drive=0 if name.startswith("Footstep_") or name == "Jump" else 1.35)
        if name in ("SkillVelocity", "SkillGravity"):
            stats["electronic_layer_db"] = -16 if name == "SkillVelocity" else -15
        report[name] = stats
        print(f"{name:20} {stats['seconds']:5.2f}s peak {peak:.2f} RMS {stats['rms']:.4f} seam {stats['seam']:.5f}")
    report_path.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(f"Validated {len(clips)} sound assets.")


if __name__ == "__main__":
    main()
