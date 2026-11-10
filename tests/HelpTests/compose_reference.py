"""Compose an IDA-coordinate asset reference; this does not render HelpView."""
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2] / "extracted_assets" / "raw"
IMAGES = ROOT / "images"


def masked(color_name, mask_name):
    color = Image.open(IMAGES / color_name).convert("RGBA")
    mask = Image.open(IMAGES / mask_name).convert("L")
    if color.size != mask.size:
        raise ValueError("mask dimensions differ: " + mask_name)
    color.putalpha(mask)
    return color


def return_label():
    lines = (ROOT / "properties" / "LawnStrings.txt").read_text(encoding="utf-16").splitlines()
    for i, line in enumerate(lines):
        if line.strip() == "[MAIN_MENU_BUTTON]":
            return next(value.strip() for value in lines[i + 1:] if value.strip())
    raise ValueError("[MAIN_MENU_BUTTON] not found")


background = Image.open(IMAGES / "background1.jpg").convert("RGBA")
background = background.resize((2800, 1200), Image.Resampling.BILINEAR)
canvas = background.crop((700, 300, 1500, 900))
canvas.alpha_composite(masked("ZombieNote.jpg", "ZombieNote_.png"), (80, 80))
canvas.alpha_composite(masked("ZombieNoteHelpBlack.png", "ZombieNoteHelp.png"), (131, 132))
canvas.alpha_composite(Image.open(IMAGES / "SeedChooser_Button.png").convert("RGBA"), (324, 500))
draw = ImageDraw.Draw(canvas)
font = ImageFont.truetype("C:/Windows/Fonts/msyhbd.ttc", 16)
label = return_label()
left, top, right, bottom = draw.textbbox((0, 0), label, font=font)
draw.text((324 + (156 - (right - left)) / 2 - left,
           499 + (42 - (bottom - top)) / 2 - top),
          label, fill=(213, 159, 43, 255), font=font)
path = HERE / "help-reference.png"
canvas.convert("RGB").save(path)
print("REFERENCE_COMPOSITE", canvas.size, path)
