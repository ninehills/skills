#!/bin/bash
# Generate a 3D morph presentation: "The Sun — Our Star"
# 3D GLB model with morph transitions, dark cinematic backgrounds
set -e

DIR="$(cd "$(dirname "$0")" && pwd)"
MODELS="$DIR/models"
OUT="$DIR/3d-model.pptx"
rm -f "$OUT"
officecli create "$OUT"
officecli open "$OUT"

###############################################################################
# SLIDES — Create all 8 slides with dark background + morph transition
###############################################################################
echo "  -> Creating 8 slides"
for i in $(seq 1 8); do
  officecli add "$OUT" / --type slide --prop background=0A0A0A --prop transition=morph
done

###############################################################################
# 3D MODELS — Sun GLB on each slide, position/rotation changes for morph
###############################################################################
echo "  -> Adding 3D sun models"
officecli add "$OUT" '/slide[1]' --type 3dmodel \
  --prop path="$MODELS/sun.glb" --prop name=sun \
  --prop x=15cm --prop y=0.5cm --prop width=18cm --prop height=18cm \
  --prop rotx=10

officecli add "$OUT" '/slide[2]' --type 3dmodel \
  --prop path="$MODELS/sun.glb" --prop name=sun \
  --prop x=0.5cm --prop y=0.5cm --prop width=16cm --prop height=16cm \
  --prop roty=50

officecli add "$OUT" '/slide[3]' --type 3dmodel \
  --prop path="$MODELS/sun.glb" --prop name=sun \
  --prop x=18cm --prop y=3cm --prop width=16cm --prop height=16cm \
  --prop roty=100 --prop rotx=15

officecli add "$OUT" '/slide[4]' --type 3dmodel \
  --prop path="$MODELS/sun.glb" --prop name=sun \
  --prop x=0.5cm --prop y=1cm --prop width=18cm --prop height=18cm \
  --prop roty=150

officecli add "$OUT" '/slide[5]' --type 3dmodel \
  --prop path="$MODELS/sun.glb" --prop name=sun \
  --prop x=17cm --prop y=0.5cm --prop width=18cm --prop height=18cm \
  --prop roty=200 --prop rotx=20

officecli add "$OUT" '/slide[6]' --type 3dmodel \
  --prop path="$MODELS/sun.glb" --prop name=sun \
  --prop x=0.5cm --prop y=2cm --prop width=17cm --prop height=17cm \
  --prop roty=250

officecli add "$OUT" '/slide[7]' --type 3dmodel \
  --prop path="$MODELS/sun.glb" --prop name=sun \
  --prop x=16cm --prop y=1cm --prop width=17cm --prop height=17cm \
  --prop roty=310 --prop rotx=10

officecli add "$OUT" '/slide[8]' --type 3dmodel \
  --prop path="$MODELS/sun.glb" --prop name=sun \
  --prop x=15cm --prop y=0.5cm --prop width=18cm --prop height=18cm \
  --prop roty=360 --prop rotx=10

###############################################################################
# SLIDE 1 — Title
###############################################################################
echo "  -> Slide 1: Title"
officecli add "$OUT" '/slide[1]' --type shape \
  --prop 'text=THE SUN' \
  --prop x=1cm --prop y=2cm --prop w=13cm --prop h=3.5cm \
  --prop size=64 --prop bold=true --prop color=FF6F00 --prop fill=00000000 \
  --prop 'font=Arial Black'

officecli add "$OUT" '/slide[1]' --type shape \
  --prop 'text=Our Star' \
  --prop x=1cm --prop y=6cm --prop w=13cm --prop h=2cm \
  --prop size=26 --prop color=FFB74D --prop fill=00000000 \
  --prop 'font=Calibri'

officecli add "$OUT" '/slide[1]' --type shape \
  --prop 'text=149.6 million km from Earth · Light takes 8 min 20 sec' \
  --prop x=1cm --prop y=8.5cm --prop w=13cm --prop h=2cm \
  --prop size=18 --prop color=9E9E9E --prop fill=00000000 \
  --prop 'font=Calibri'

###############################################################################
# SLIDE 2 — Star Profile
###############################################################################
echo "  -> Slide 2: Star Profile"
officecli add "$OUT" '/slide[2]' --type shape \
  --prop 'text=Star Profile' \
  --prop x=18cm --prop y=1cm --prop w=15cm --prop h=2.5cm \
  --prop size=40 --prop bold=true --prop color=FF6F00 --prop fill=00000000 \
  --prop 'font=Calibri' --prop align=right

officecli add "$OUT" '/slide[2]' --type shape \
  --prop 'text=Spectral type  G2V yellow dwarf\nDiameter  1.392 million km\nMass  330,000x Earth\nSurface temp  5,778 K\nCore temp  15 million K\nAge  4.6 billion years' \
  --prop x=18cm --prop y=4cm --prop w=15cm --prop h=14cm \
  --prop size=22 --prop color=E0E0E0 --prop fill=00000000 \
  --prop 'font=Calibri' --prop align=right --prop lineSpacing=2x

###############################################################################
# SLIDE 3 — Internal Structure
###############################################################################
echo "  -> Slide 3: Internal Structure"
officecli add "$OUT" '/slide[3]' --type shape \
  --prop 'text=Internal Structure' \
  --prop x=1cm --prop y=1cm --prop w=15cm --prop h=2.5cm \
  --prop size=40 --prop bold=true --prop color=FF6F00 --prop fill=00000000 \
  --prop 'font=Calibri'

officecli add "$OUT" '/slide[3]' --type shape \
  --prop 'text=Core  Hydrogen fuses into helium\nRadiative zone  Photons take 170,000 years\nConvective zone  Plasma churns upward\nPhotosphere  The visible "surface"\nCorona  Temperature mystery: millions of degrees' \
  --prop x=1cm --prop y=4cm --prop w=16cm --prop h=14cm \
  --prop size=22 --prop color=E0E0E0 --prop fill=00000000 \
  --prop 'font=Calibri' --prop lineSpacing=2x

###############################################################################
# SLIDE 4 — Solar Activity
###############################################################################
echo "  -> Slide 4: Solar Activity"
officecli add "$OUT" '/slide[4]' --type shape \
  --prop 'text=Solar Activity' \
  --prop x=20cm --prop y=1cm --prop w=13cm --prop h=2.5cm \
  --prop size=40 --prop bold=true --prop color=FF6F00 --prop fill=00000000 \
  --prop 'font=Calibri' --prop align=right

officecli add "$OUT" '/slide[4]' --type shape \
  --prop 'text=Sunspots  Cool regions twisted by magnetic fields\nFlares  Energy of a billion H-bombs in seconds\nCMEs  A billion tons of plasma ejected\nSolar wind  Particles at 400 km/s' \
  --prop x=20cm --prop y=4cm --prop w=13cm --prop h=14cm \
  --prop size=22 --prop color=E0E0E0 --prop fill=00000000 \
  --prop 'font=Calibri' --prop align=right --prop lineSpacing=2x

###############################################################################
# SLIDE 5 — Source of Life
###############################################################################
echo "  -> Slide 5: Source of Life"
officecli add "$OUT" '/slide[5]' --type shape \
  --prop 'text=Source of Life' \
  --prop x=1cm --prop y=1cm --prop w=14cm --prop h=2.5cm \
  --prop size=40 --prop bold=true --prop color=FF6F00 --prop fill=00000000 \
  --prop 'font=Calibri'

officecli add "$OUT" '/slide[5]' --type shape \
  --prop 'text=Drives climate and water cycles\nEnergy source for photosynthesis\nMagnetosphere shields from cosmic rays\nAurora — a romantic gift from solar wind' \
  --prop x=1cm --prop y=4cm --prop w=14cm --prop h=14cm \
  --prop size=22 --prop color=E0E0E0 --prop fill=00000000 \
  --prop 'font=Calibri' --prop lineSpacing=2x

###############################################################################
# SLIDE 6 — Observation History
###############################################################################
echo "  -> Slide 6: Observation History"
officecli add "$OUT" '/slide[6]' --type shape \
  --prop 'text=Observation History' \
  --prop x=19cm --prop y=1cm --prop w=14cm --prop h=2.5cm \
  --prop size=40 --prop bold=true --prop color=FF6F00 --prop fill=00000000 \
  --prop 'font=Calibri' --prop align=right

officecli add "$OUT" '/slide[6]' --type shape \
  --prop 'text=1613  Galileo records sunspots\n1868  Helium discovered\n1995  SOHO satellite launched\n2018  Parker Solar Probe touches the Sun' \
  --prop x=19cm --prop y=4cm --prop w=14cm --prop h=14cm \
  --prop size=22 --prop color=E0E0E0 --prop fill=00000000 \
  --prop 'font=Calibri' --prop align=right --prop lineSpacing=2x

###############################################################################
# SLIDE 7 — Future of the Sun
###############################################################################
echo "  -> Slide 7: Future of the Sun"
officecli add "$OUT" '/slide[7]' --type shape \
  --prop 'text=Future of the Sun' \
  --prop x=1cm --prop y=1cm --prop w=14cm --prop h=2.5cm \
  --prop size=40 --prop bold=true --prop color=FF6F00 --prop fill=00000000 \
  --prop 'font=Calibri'

officecli add "$OUT" '/slide[7]' --type shape \
  --prop 'text=In 5 billion years, expands into a red giant\nSwallows Mercury and Venus, scorches Earth\nOuter layers form a planetary nebula\nCore collapses into a white dwarf' \
  --prop x=1cm --prop y=4cm --prop w=14cm --prop h=14cm \
  --prop size=22 --prop color=E0E0E0 --prop fill=00000000 \
  --prop 'font=Calibri' --prop lineSpacing=2x

###############################################################################
# SLIDE 8 — Closing
###############################################################################
echo "  -> Slide 8: Closing"
officecli add "$OUT" '/slide[8]' --type shape \
  --prop 'text=Per Aspera Ad Astra' \
  --prop x=1cm --prop y=7cm --prop w=13cm --prop h=3cm \
  --prop size=48 --prop bold=true --prop italic=true --prop color=FF6F00 --prop fill=00000000 \
  --prop 'font=Georgia'

officecli add "$OUT" '/slide[8]' --type shape \
  --prop 'text=Through hardships to the stars' \
  --prop x=1cm --prop y=11cm --prop w=13cm --prop h=2cm \
  --prop size=24 --prop color=9E9E9E --prop fill=00000000 \
  --prop 'font=Calibri'

###############################################################################
# FINALIZE
###############################################################################
officecli close "$OUT"
officecli validate "$OUT"
echo "Generated: $OUT"
