# Steam release notes: Nice Health Tab: Recolorized

Steam Workshop BBCode: copy the text inside each block into the "Change Notes" or description field.

## English

```
[h1]Big settings update: tabs, per-mod colors, live preview[/h1]

[h2]Settings[/h2]
[list]
[*][b]New settings window[/b] with tabs: General / Tier colors / DLC / Mods / Fine-tune. Every tab has its own reset button.
[*][b]Live preview[/b] on Nice Health Tab's own doll (in-game only): one implant per tier and mod, a tooltip with part, implant and mod name, and a "Show organs / Show body" button.
[*][b]Palette profiles[/b]: save, copy, import and export color sets as JSON. Profiles also keep mod colors, detected tiers and color links.
[*][b]Palette icon[/b] in the corner of the health tab doll opens the settings.
[/list]

[h2]Colors[/h2]
[list]
[*][b]Per-mod colors[/b]: every mod that adds implants or prosthetics gets its own row. Auto = colored by tier.
[*][b]New tiers are detected automatically[/b] from other mods and shown with their source mod. Integrated Implants is treated as a regular mod.
[*][b]Color inheritance[/b]: a DLC, tier or mod can reuse the color of another tier, DLC or mod (the "▾" button in the row).
[*][b]Damage color[/b] (optional): two levels like NHT (painful / critical). "Hue only" keeps NHT's saturation. The default matches NHT.
[*][b]Damaged implants[/b] show the damage color instead of the tier color.
[*]Pre-colored prosthetic textures of this mod are neutralized, so tinting gives the real tier color.
[/list]

[h2]Health tab[/h2]
[list]
[*][b]Organs view:[/b] implants that replace organs (bionic kidney, stomach, lung, jaw, nose) now appear in the list next to the doll.
[*][b]Bones view:[/b] only spine, pelvis and ribs, plus other bones that are damaged or have an implant. Missing bones and bones under a missing limb are hidden. Ribs show only in the bones view.
[/list]

[h2]Under the hood[/h2]
[list]
[*]Faster doll rendering: cached tiers, limb slots and part kinds, no per-frame allocations.
[*]18 languages. New strings are machine-translated, corrections are welcome.
[/list]

[i]Requires Nice Health Tab. Safe to add to an existing save.[/i]
```

## Русский

```
[h1]Большое обновление настроек: вкладки, цвета по модам, живой предпросмотр[/h1]

[h2]Настройки[/h2]
[list]
[*][b]Новое окно настроек[/b] с вкладками: Общие / Цвета тиров / DLC / Моды / Тонкая настройка. На каждой вкладке своя кнопка сброса.
[*][b]Живой предпросмотр[/b] на родной кукле Nice Health Tab (только в игре): по одному импланту на тир и мод, тултип с названием части, импланта и мода, кнопка «Показать органы / тело».
[*][b]Профили палитр[/b]: сохраняйте, копируйте, импортируйте и экспортируйте наборы цветов в JSON. В профилях хранятся цвета модов, найденных тиров и связи цветов.
[*][b]Иконка палитры[/b] в углу куклы вкладки здоровья открывает настройки.
[/list]

[h2]Цвета[/h2]
[list]
[*][b]Цвета по модам[/b]: у каждого мода с имплантами или протезами своя строка. «Авто» значит окраска по тиру.
[*][b]Новые тиры других модов находятся автоматически[/b], с указанием мода-источника. Integrated Implants считается обычным модом.
[*][b]Наследование цвета[/b]: DLC, тир или мод может брать цвет другого тира, DLC или мода (кнопка «▾» в строке).
[*][b]Цвет повреждений[/b] (по желанию): два уровня как в NHT (болезненное / критическое). Режим «Только тон» сохраняет насыщенность NHT. Цвет по умолчанию совпадает с NHT.
[*][b]Повреждённые импланты[/b] показываются цветом повреждения, а не цветом тира.
[*]Предокрашенные текстуры протезов этого мода нейтрализуются, поэтому окраска даёт настоящий цвет тира.
[/list]

[h2]Вкладка здоровья[/h2]
[list]
[*][b]Вид органов:[/b] импланты, заменившие органы (бионические почка, желудок, лёгкое, челюсть, нос), теперь есть в списке рядом с куклой.
[*][b]Вид костей:[/b] только позвоночник, таз и рёбра, плюс другие кости при повреждении или с имплантом. Отсутствующие кости и кости под отсутствующей конечностью скрыты. Рёбра показываются только в виде костей.
[/list]

[h2]Под капотом[/h2]
[list]
[*]Быстрее отрисовка куклы: кэш тиров, слотов конечностей и типов частей, без аллокаций каждый кадр.
[*]18 языков. Новые строки переведены машинным переводом, поправки приветствуются.
[/list]

[i]Нужен Nice Health Tab. Можно добавлять в существующее сохранение.[/i]
```
