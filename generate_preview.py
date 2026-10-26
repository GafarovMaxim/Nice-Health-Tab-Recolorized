import os
import shutil
import numpy as np
from PIL import Image, ImageDraw, ImageFont

def generate_master_preview():
    # Source paths
    p1_path = r'E:\SteamLibrary\steamapps\workshop\content\294100\3328729902\About\Preview.png'
    p2_path = r'E:\SteamLibrary\steamapps\workshop\content\294100\3701712250\About\Preview.png'
    m_path  = r'C:\Users\gfrov\.gemini\antigravity\brain\047981fc-fcc7-4f91-a00a-a3c118ef53c9\scratch\kra_extracted\mergedimage.png'

    p1 = Image.open(p1_path).convert('RGBA')
    p2 = Image.open(p2_path).convert('RGBA')
    m  = Image.open(m_path).convert('RGBA')
    width, height = p1.size

    # Color Palette Tokens
    COLOR_ADVBIO = (180, 216, 245)      # Light Blue #B4D8F5
    COLOR_ULTRA  = (113, 203, 193)      # Mint Turquoise #71CBC2
    COLOR_ARCHO  = (175, 181, 142)      # Sage Green #AFB58E
    COLOR_ACCENT = (161, 206, 243)      # Accent Blue #A1CEF3 for YAPEM PATCH

    # 1. BUILD DOLL LIMB BARRIER MASK ON CLEAN P1
    barrier = Image.new('L', (width, height), 255)
    for y in range(height):
        for x in range(400, width):
            p = p1.getpixel((x, y))
            if p[0] < 80 and p[1] < 80 and p[2] < 80:
                barrier.putpixel((x, y), 0)

    seeds_archo = [(515, 180), (485, 260), (465, 315)]
    seeds_advbio = [(629, 180), (650, 240), (675, 315)]
    seeds_ultra = [(550, 350), (595, 350)]

    for s in seeds_archo: ImageDraw.floodfill(barrier, s, 1)
    for s in seeds_advbio: ImageDraw.floodfill(barrier, s, 2)
    for s in seeds_ultra: ImageDraw.floodfill(barrier, s, 3)

    # 2. LOAD & PREPARE AUTHENTIC PROSTHESIS TEXTURES
    tex_archo_arm = Image.open(r'G:\Modding\RimWorld\NiceHealthTab_YAPEM_Patch\Textures\HediffTab\Prosthesis\Female\Arms\ArchotechArm.png').convert('RGBA')
    tex_adv_arm   = Image.open(r'G:\Modding\RimWorld\NiceHealthTab_YAPEM_Patch\Textures\HediffTab\Prosthesis\Female\Arms\AdvBionicArm.png').convert('RGBA')
    tex_ultra_leg = Image.open(r'G:\Modding\RimWorld\NiceHealthTab_YAPEM_Patch\Textures\HediffTab\Prosthesis\Female\Legs\UltratechLeg.png').convert('RGBA')

    # Archotech Left Arm (viewer's left: FLIP_LEFT_RIGHT to fit left arm pose)
    arr_archo = np.array(tex_archo_arm.transpose(Image.Transpose.FLIP_LEFT_RIGHT))
    ys_a, xs_a = np.where(arr_archo[:, :, 3] > 10)
    crop_archo = Image.fromarray(arr_archo).crop((xs_a.min(), ys_a.min(), xs_a.max()+1, ys_a.max()+1))
    scaled_archo = crop_archo.resize((82, 188), Image.Resampling.LANCZOS)

    # Advanced Bionic Right Arm (viewer's right: original orientation)
    arr_adv = np.array(tex_adv_arm)
    ys_b, xs_b = np.where(arr_adv[:, :, 3] > 10)
    crop_adv = Image.fromarray(arr_adv).crop((xs_b.min(), ys_b.min(), xs_b.max()+1, ys_b.max()+1))
    scaled_adv = crop_adv.resize((82, 188), Image.Resampling.LANCZOS)

    # Ultratech Legs (left flipped, right original)
    arr_leg = np.array(tex_ultra_leg)
    ys_l, xs_l = np.where(arr_leg[:, :, 3] > 10)
    crop_leg_base = Image.fromarray(arr_leg).crop((xs_l.min(), ys_l.min(), xs_l.max()+1, ys_l.max()+1))
    w_leg = 64
    h_leg = 330
    leg_left_s  = crop_leg_base.transpose(Image.Transpose.FLIP_LEFT_RIGHT).resize((w_leg, h_leg), Image.Resampling.LANCZOS)
    leg_right_s = crop_leg_base.resize((w_leg, h_leg), Image.Resampling.LANCZOS)

    # Composite authentic limbs onto temporary transparent canvas
    limb_canvas = Image.new('RGBA', (width, height), (0, 0, 0, 0))
    limb_canvas.paste(scaled_archo, (453, 134), scaled_archo)
    limb_canvas.paste(scaled_adv, (610, 134), scaled_adv)
    limb_canvas.paste(leg_left_s, (510, 268), leg_left_s)
    limb_canvas.paste(leg_right_s, (570, 268), leg_right_s)

    # Blend limbs inside barrier mask preserving doll's original glowing outline
    arr_p1 = np.array(p1)
    arr_limb = np.array(limb_canvas)
    arr_barrier = np.array(barrier)

    for y in range(height):
        for x in range(400, width):
            b = arr_barrier[y, x]
            if b in [1, 2, 3]:
                l_alpha = arr_limb[y, x, 3] / 255.0
                if l_alpha > 0.05:
                    orig = arr_p1[y, x, :3]
                    limb_rgb = arr_limb[y, x, :3]
                    arr_p1[y, x, :3] = (limb_rgb * l_alpha + orig * (1 - l_alpha)).astype(np.uint8)
                else:
                    orig = arr_p1[y, x]
                    target_col = COLOR_ARCHO if b == 1 else (COLOR_ADVBIO if b == 2 else COLOR_ULTRA)
                    lum = (int(orig[0]) + int(orig[1]) + int(orig[2])) / (3.0 * 255.0)
                    arr_p1[y, x, 0] = int(target_col[0] * lum)
                    arr_p1[y, x, 1] = int(target_col[1] * lum)
                    arr_p1[y, x, 2] = int(target_col[2] * lum)

    p1 = Image.fromarray(arr_p1)

    # 3. EYE FROM P2 (RECOLORED TO ARCHOTECH SAGE)
    eye = p2.crop((570, 75, 605, 105))
    e_arr = np.array(eye)
    for y in range(e_arr.shape[0]):
        for x in range(e_arr.shape[1]):
            r, g, b, a = e_arr[y, x]
            if g > 120 and r < 100 and b < 100:
                lum = g / 255.0
                e_arr[y, x] = [int(COLOR_ARCHO[0] * lum), int(COLOR_ARCHO[1] * lum), int(COLOR_ARCHO[2] * lum), a]
    eye_archo = Image.fromarray(e_arr)
    p1.paste(eye_archo, (570, 75))

    # 4. DIAMOND 1 ICON (PURE WHITE BIONIC ARM, ZERO PURPLE/BORDER ARTIFACTS)
    draw = ImageDraw.Draw(p1)
    draw.polygon([(189, 45), (212, 74), (189, 95), (166, 74)], fill=(13, 13, 13, 255))

    arm_crop = m.crop((97, 138, 130, 159))
    arm_rgba = Image.new('RGBA', arm_crop.size, (0, 0, 0, 0))
    for y in range(arm_crop.height):
        for x in range(arm_crop.width):
            r, g, b, a = arm_crop.getpixel((x, y))
            if (x > 30 and y > 18) or (x > 31 and y < 3):
                continue
            if r > 20 and g > 20 and b > 20:
                lum = (r + g + b) / 3.0
                alpha = max(0, min(255, int((lum - 13) * 255.0 / (255 - 13))))
                arm_rgba.putpixel((x, y), (255, 255, 255, alpha))

    p1.paste(arm_rgba, (173, 64), arm_rgba)

    # 5. CHEVRONS (STRICT PIXEL BOUNDS ONLY)
    for y in range(43, 99):
        for x in range(185, 227):
            p = p1.getpixel((x, y))
            if p[2] > 140 and (p[2] - p[0]) > 70:
                lum = p[2] / 255.0
                p1.putpixel((x, y), (int(COLOR_ADVBIO[0]*lum), int(COLOR_ADVBIO[1]*lum), int(COLOR_ADVBIO[2]*lum), p[3]))

    for y in range(120, 176):
        for x in range(110, 149):
            p = p1.getpixel((x, y))
            if p[0] > 70 and p[2] > 100 and p[1] < 120 and p != (13, 13, 13, 255):
                lum = (p[0] + p[2]) / (2.0 * 255.0)
                p1.putpixel((x, y), (int(COLOR_ULTRA[0]*lum), int(COLOR_ULTRA[1]*lum), int(COLOR_ULTRA[2]*lum), p[3]))

    for y in range(197, 253):
        for x in range(185, 227):
            p = p1.getpixel((x, y))
            if p[2] > 50 and p[0] < 30 and p[1] < 70 and p != (13, 13, 13, 255):
                lum = min((p[2] * 2.0) / 255.0, 1.0)
                p1.putpixel((x, y), (int(COLOR_ARCHO[0]*lum), int(COLOR_ARCHO[1]*lum), int(COLOR_ARCHO[2]*lum), p[3]))

    # 6. CLEAR RIBBON TEXT (CLEAN RECTANGLES FLUSH WITH DOLL OUTLINE)
    arr_p1 = np.array(p1)
    ribbons_clean = [
        (43, 99, 238, 500),    # Ribbon 1: y in [43..99], search_start x=500
        (120, 176, 155, 485),  # Ribbon 2: y in [120..176], search_start x=485
        (197, 253, 238, 468)   # Ribbon 3: y in [197..253], search_start x=468
    ]

    for y_start, y_end, x_start, search_start in ribbons_clean:
        for y in range(y_start, y_end + 1):
            white_x = 540
            for x in range(search_start, 540):
                # Doll outline has high brightness
                if arr_p1[y, x, 0] > 170 and arr_p1[y, x, 1] > 170 and arr_p1[y, x, 2] > 170:
                    white_x = x
                    break
            arr_p1[y, x_start:white_x] = [13, 13, 13, 255]

    p1 = Image.fromarray(arr_p1)
    draw = ImageDraw.Draw(p1)

    # 7. RIBBON TEXT (AUTHENTIC FRANKLIN GOTHIC STYLE)
    font_rib = ImageFont.truetype(r'C:\Windows\Fonts\framd.ttf', 27)
    draw.text((240, 57), 'ADVANCED BIONICS', font=font_rib, fill=(255, 255, 255), stroke_width=1, stroke_fill=(255, 255, 255))
    draw.text((158, 134), 'ULTRATECH LIMBS', font=font_rib, fill=(255, 255, 255), stroke_width=1, stroke_fill=(255, 255, 255))
    draw.text((240, 211), 'ARCHOTECH FIX', font=font_rib, fill=(255, 255, 255), stroke_width=1, stroke_fill=(255, 255, 255))

    # 8. VERSION 1.7 BADGE (SPLIT GLYPHS + CANONICAL ROUND DOT)
    for x in range(15, 155):
        c_top = np.array(p1.getpixel((x, 18))[:3], dtype=float)
        c_bot = np.array(p1.getpixel((x, 105))[:3], dtype=float)
        for y in range(19, 105):
            t = (y - 18) / (105.0 - 18.0)
            c = (1 - t) * c_top + t * c_bot
            p1.putpixel((x, y), (int(c[0]), int(c[1]), int(c[2]), 255))

    font_ver = ImageFont.truetype(r'C:\Windows\Fonts\ariblk.ttf', 58)
    draw.text((20, 26), '1', font=font_ver, fill=(255, 255, 255), stroke_width=6, stroke_fill=(0, 0, 0))
    draw.text((82, 26), '7', font=font_ver, fill=(255, 255, 255), stroke_width=6, stroke_fill=(0, 0, 0))

    # Canonical round dot with black outline
    dot_cx, dot_cy = 64.0, 83.5
    draw.ellipse([dot_cx - 12.0, dot_cy - 12.0, dot_cx + 12.0, dot_cy + 12.0], fill=(0, 0, 0))
    draw.ellipse([dot_cx - 6.5, dot_cy - 6.5, dot_cx + 6.5, dot_cy + 6.5], fill=(255, 255, 255))

    # 9. TITLE SUBTITLE 'RECOLORIZED'
    font_sub = ImageFont.truetype(r'C:\Windows\Fonts\ariblk.ttf', 18)
    def draw_spaced(pos, text, font, fill, stroke_fill, stroke_width, spacing=3):
        x, y = pos
        for char in text:
            draw.text((x, y), char, font=font, fill=fill, stroke_width=stroke_width, stroke_fill=stroke_fill)
            x += font.getlength(char) + spacing

    draw_spaced((38, 376), 'RECOLORIZED', font_sub, COLOR_ACCENT, (0, 0, 0), 3, spacing=3)

    return p1

if __name__ == '__main__':
    out = generate_master_preview()
    destinations = [
        r'G:\Modding\RimWorld\NiceHealthTab_YAPEM_Patch\About\Preview.png',
        r'E:\SteamLibrary\steamapps\common\RimWorld\Mods\NiceHealthTab_YAPEM_Patch\About\Preview.png',
        r'C:\Users\gfrov\.gemini\antigravity\brain\047981fc-fcc7-4f91-a00a-a3c118ef53c9\Preview.png'
    ]
    for dest in destinations:
        dest_dir = os.path.dirname(dest)
        if os.path.exists(dest_dir):
            out.save(dest)
            print(f'Saved preview to: {dest}')
