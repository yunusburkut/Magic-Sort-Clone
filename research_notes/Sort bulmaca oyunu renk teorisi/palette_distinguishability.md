# Categorical liquid palette (8-12 colours) that stays distinguishable, including under colour-vision deficiency

How the numbers in this file were made (read first):
- **Hex values** for published palettes come from the cited sources.
- **All OKLCH, OKLab ΔE (ΔEOK), CIEDE2000 (ΔE00) and simulated-CVD numbers were computed by the researcher.** No source supplies them.
  - OKLab uses Björn Ottosson's published linear-sRGB matrices.
  - CIEDE2000 follows the Sharma formulation. It was checked against Sharma test pairs #1 (2.0425) and #17 (27.1492). Both matched to 4 decimals.
  - #FF0000 came out as oklch(0.628 0.258 29.23) and CIELAB (53.24, 80.09, 67.20). Both match the standard reference values.
  - CVD was simulated with the Machado-Oliveira-Fernandes 2009 matrices at severity 1.0. They were applied in linear sRGB, with output clamped to [0,1].
- **"Worst-case"** means the minimum over four conditions: normal vision, protanopia, deuteranopia and tritanopia.
- **The "combined" metric** is min(ΔEOK, ΔE00/100). It makes a pair pass both metrics.
- **Script:** a C# file-based app, `pal.cs`, run with `dotnet run pal.cs`. It lives in the session scratchpad at `C:\Users\Burkut\AppData\Local\Temp\claude\C--GitHub-Magic-Sort-Clone\098a0732-eab3-4afa-8dd4-beaf2b33333a\scratchpad\pal\`. The scratchpad is temporary, so copy the script into the repo if it must be kept.
- **Assumed background:** oklch(0.25 0.04 270), a dark blue-grey. It was used only for the contrast columns.

---

## Q1. What OKLab / CIEDE2000 pairwise distance reads as "clearly different" on a small phone screen, and how much should lightness vary?

### Takeaway
A JND is about ΔEOK 0.02, or about 1 ΔE00. That is far too small for at-a-glance category matching, and no standard gives a categorical threshold.
- **Benchmark targets.** Computed from palettes that experts call colour-blind-safe:
  - Normal vision: aim for **≥ 0.10–0.12 ΔEOK (ΔE00 ≳ 15)** between every pair.
  - Each simulated CVD type: aim for **≥ ~0.075–0.08 ΔEOK (ΔE00 ≳ 8–10)**. Okabe-Ito's worst pair is 0.076.
  - Pairs under ~0.04 ΔEOK in simulation collapse in practice.
- **Lightness should cover a wide span.** Use OKLab L ≈ 0.45–0.97, with about 0.5 between the darkest and lightest liquid. Under red-green CVD, lightness and the blue-yellow axis are the only dimensions left.
- **Small marks make it worse.** Small stimuli need bigger differences, and small fields specifically weaken blue-yellow discrimination.

### Cited Findings
- **OKLab origin.** Ottosson published OKLab on 23 Dec 2020 and updated the matrices on 2021-01-25. It was designed because CIELAB/CIELUV predict hue poorly, especially for blues. It keeps IPT's structure while approaching CAM16-UCS lightness and chroma prediction. — [Ottosson, "A perceptual color space for image processing"](https://bottosson.github.io/posts/oklab/)
- **ΔEOK JND.** CSS Color 4 gamut mapping uses a just-noticeable difference of **0.02 in ΔEOK**. Reference implementations use `const JND = 0.02; ε = 0.0001`. ΔEOK values run at about 1/100 of ΔE00, so "dEOk's JND is 0.02, not 2.0". — [Keith Cirkel, "too much color"](https://www.keithcirkel.co.uk/too-much-color); [W3C csswg-drafts #7135 discussion](https://lists.w3.org/Archives/Public/public-css-archive/2022Jul/0083.html); [Servo/Firefox gamut.rs](https://searchfox.org/firefox-main/source/servo/components/style/color/gamut.rs)
- **CSS Color 4 status.** The spec is a W3C Candidate Recommendation Draft dated 20 October 2026. It defines OKLab/OKLCH and ΔEOK (§20.3). — [W3C CSS Color 4](https://www.w3.org/TR/css-color-4/)
- **OKLCH vs LCH.** OKLCH keeps hue stable when only chroma changes. CIE LCH shows "an unexpected hue shift" for blues between hues 270 and 330: `lch(0.35 110 300)` looks blue but turns purple at chroma 75. — [Evil Martians, "OKLCH in CSS" (metadata date 2025-09-17)](https://evilmartians.com/chronicles/oklch-in-css-why-quit-rgb-hsl)
- **HSL is not a design space.** "HSL lightness is not consistent across hue axes." Practical sRGB/P3 chroma stays below about 0.37. P3 adds about 30% more visible colours than sRGB. — [Evil Martians](https://evilmartians.com/chronicles/oklch-in-css-why-quit-rgb-hsl)
- **Lab assumes a "simple world".** Szafir's CIELAB modelling assumes 1 unit ≈ 1 JND. Visualisations violate that assumption (isolation, geometry, viewing distance, display). Discriminability "varies significantly across mark types" (points, bars, lines). Size was a tested variable. — [Szafir, "Modeling Color Difference for Visualization Design", IEEE TVCG 24(1) 2018, PubMed 28866544](https://pubmed.ncbi.nlm.nih.gov/28866544/); [project page](https://cmci.colorado.edu/visualab/VisColors/)
- **Web viewing thresholds.** An earlier paper extends CIELAB to crowdsourced web viewing. — [Szafir, Stone & Gleicher, "Adapting Color Difference for Design", CIC 2014](https://www.tableau.com/de-de/research/publications/adapting-color-difference-design)
  - An *auto-generated* Academia.edu summary gives 50%-noticeable differences of about ΔL 4.06, Δa 5.26, Δb 5.88 CIELAB units. **Unverified against the paper.** — [Academia.edu copy](https://www.academia.edu/9828995/Adapting_Color_Difference_for_Design)
- **Small-field tritanopia.** Small or brief foveal stimuli make normal observers confuse colours in a tritan-like way, and the effect shrinks as the field grows. The central zone lacking S-cone function is about 18–25′ of arc. These details come from search-result abstracts, not full texts (1969–2004). — [Williams et al. 1981, Vision Research](https://aria.cvs.rochester.edu/papers/williams-etal_VR1981b.pdf); [JOSA 1969](https://www.doi.org/10.1364/JOSA.59.000640); [BJO 2003](https://bjo.bmj.com/content/87/6/742)
- **Optimiser thresholds are design choices.** The `palettecore` package's categorical optimiser maximises the minimum pairwise distance across normal vision and all three Machado-simulated CVD types. It states that its thresholds are "configurable design rules, not established accessibility cut-offs". — [palettecore (CRAN)](https://cran.rstudio.com/web/packages/palettecore/index.html)
- **Maximin selection.** Glasbey et al. choose categorical colours that are "as distinct as possible" by maximising Euclidean CIELAB distance, either sequentially or by simulated annealing. — [Glasbey, van der Heijden, Toh & Gray, "Colour displays for categorical images", Color Res. Appl. 2007](https://strathprints.strath.ac.uk/30312/); [glasbey Python package](https://glasbey.readthedocs.io/en/latest/)

### Inferences
- **There is no published "clearly different at a glance" number.** All sources measure JND (0.02 ΔEOK / ~1 ΔE00) or leave thresholds to the designer. Targets are best set empirically, from the worst pair of palettes experts accept as CVD-safe. Computed benchmarks follow, with min ΔEOK per condition:

  | Palette | normal | protan | deutan | tritan (Machado, approximate) |
  |---|---|---|---|---|
  | Okabe-Ito (7 chromatic) | 0.156 | 0.096 | **0.076** | 0.085 |
  | Tol bright (7) | 0.106 | 0.072 | 0.079 | 0.033 (blue/green) |
  | Tol vibrant (7) | 0.129 | 0.085 | 0.062 | 0.061 |
  | Tol muted (10) | 0.120 | 0.081 | 0.052 | 0.073 |
  | IBM (5) | 0.114 | 0.107 | 0.084 | 0.052 |
  | Tableau 10 | 0.083 | **0.011** | **0.007** (red/green) | 0.051 |
  | Kelly 20 chromatic | 0.051 | **0.004** | **0.009** | 0.026 |
  | This project's current 10 | 0.142 | **0.033** (Red/Brown) | **0.032** (Blue/Purple, Green/Orange) | 0.055 |

- **Practical targets for a 12-colour game palette.**
  - **Normal vision:** min ΔEOK ≥ 0.10 (ΔE00 ≥ 15).
  - **Each of protan, deutan and tritan:** min ΔEOK ≥ 0.075 (ΔE00 ≥ 8). This is "Okabe-Ito grade".
  - **Hard failure:** anything under 0.04 ΔEOK in simulation, such as Tableau's red/green (0.007, ΔE00 0.7).
  - **Phone-size margin:** liquid bands in tubes are small, maybe ~1–2° of visual angle. Szafir's work and small-field tritanopia both say small areas need more difference, so add margin, especially for blue-vs-green, yellow-vs-white and yellow-vs-lime.
- **Lightness strategy.**
  - **Why lightness matters:** under protan/deutan simulation the red-green opponent axis largely disappears. What remains is roughly L plus the blue-yellow axis. Every pair must therefore differ clearly in L or in yellow-vs-blue.
  - **Examples:** Tableau red (L 0.638) and green (L 0.642) differ only on red-green and collapse. Okabe-Ito vermillion (L 0.621) and bluish-green (L 0.620) share L yet survive (0.110 deutan), because the green is pushed toward blue (h 165).
  - **Spread to use:** the computed recommended palette spans L 0.456–0.964. Okabe-Ito spans 0.53–0.90. Use the full range the background allows.
- **The dark background costs the bottom third of the L axis.** Liquids below L ≈ 0.45 have WCAG contrast under about 2:1 against an L 0.25 background. Navy, maroon and forest green therefore become weak categories.
  - **Project-specific constraint:** the "hidden liquid" colour is `_hiddenLiquidColor = (0.0566, 0.0566, 0.0566)`, about #0E0E0E (L 0.164). It sits in `C:\GitHub\Magic-Sort-Clone\Assets\HeronCaseRepo\Scripts\Data\GameSettings.cs`. Any dark liquid also risks being read as "hidden".

### Gaps
- No peer-reviewed source states a minimum ΔEOK or ΔE00 for *categorical, at-a-glance* identification on phones. The full Szafir 2018 paper (size-dependent ND model) could not be retrieved (connection refused), so its size-scaling coefficients are not quoted.
- The JND-of-0.02 claim is a CSS gamut-mapping engineering constant, not a psychophysical result for large differences.
- None of the tested metrics is validated for large suprathreshold differences. Colorgorical-style "name difference" is the usual complement (see Q3).

---

## Q2. Which hue pairs collapse under deuteranopia/protanopia/tritanopia, and how do vivid game palettes avoid it?

### Takeaway
- **Protan/deutan** collapse red, orange, brown, olive, yellow-green and green into one yellow-brown family, separated only by lightness. They also collapse blue and purple, and pink vs grey or cyan.
- **Tritan** collapses blue vs green or teal, yellow vs white or pink, and orange vs pink or magenta.
- **What survives:**
  - colour pairs that differ in **lightness**, or along **blue-vs-yellow**;
  - **greens pushed toward blue-green** and **reds pushed toward orange-vermillion**;
  - **purples made lighter or redder than the blue**;
  - and, past about 7–8 colours, a **non-colour cue**.

### Cited Findings
- **XAG 102:** "Red and green together commonly fail for players with common colorblindness. Pair color with shape / text." — [Xbox XAG 102 Contrast (search-result excerpt)](https://devdocs.xbox.com/build/game-principles/accessibility/xag-deep-dives/xag-102-contrast.md)
- **Apple HIG:** "people who are color blind may have particular difficulty with pairings such as red-green and blue-orange". — [Apple HIG, Accessibility](https://developer.apple.com/design/human-interface-guidelines/accessibility)
- **GAG on brightness and two-team colours:**
  - Red/green difficulty is most common.
  - "Some colours look darker to colourblind players, especially red."
  - For two teams, "orange vs. blue works for the three most common types", and "adding a brightness difference (light blue vs. dark orange) covers more edge cases".
  - — [Game Accessibility Guidelines](https://gameaccessibilityguidelines.com/ensure-no-essential-information-is-conveyed-by-a-colour-alone/)
- **What each type sees (practitioner description):**
  - People with ~1% missing red or green cones see red/green as shades of yellow and purple as shades of blue. Orange looks like "a darker yellow".
  - In the ~0.01% blue-cone group, "Yellow becomes light grey, and blue becomes dark grey".
  - — [Sasha Trubetskoy, "List of 20 Simple, Distinct Colors"](https://sashamaps.net/docs/resources/20-colors/)
- **Simulation method:**
  - Machado, Oliveira & Fernandes 2009 matrices at severity 1.0 (the matrices used here). Protan: [[0.152286, 1.052583, −0.204868], [0.114503, 0.786281, 0.099216], [−0.003882, −0.048116, 1.051998]]; the deutan and tritan matrices are in the source. — [colorspacious cvd.py](https://raw.githubusercontent.com/njsmith/colorspacious/master/colorspacious/cvd.py)
  - DaltonLens (Oct/Nov 2021) review:
    - For protanopia and deuteranopia, Viénot 1999 and Brettel 1997 are best, with a slight edge to Viénot.
    - For tritanopia, Brettel 1997 is "essentially the only valid choice".
    - Machado 2009 "does not work well for tritanopia". It is best kept for anomalous trichromacy.
    - Coblis V1 "should never be used".
    - Models expect linear light; skipping sRGB decoding makes colours far too dark.
    - — [DaltonLens, "Review of Open Source Color Blindness Simulations"](https://daltonlens.org/opensource-cvd-simulation/)
- **Simulator tools:** GAG recommends Colour Oracle (Windows/Mac/Linux), a real-time Android simulator and Unreal's built-in simulator. It advises designing for 100% severity, which also covers milder cases. — [GAG](https://gameaccessibilityguidelines.com/ensure-no-essential-information-is-conveyed-by-a-colour-alone/)

### Inferences
- **Observed collapses (worst simulated pairs).** Computed with Machado 2009 at severity 1.0, in linear sRGB:
  - **Deutan:**
    - Tableau red #E15759 / green #59A14F: ΔEOK 0.007, ΔE00 0.7
    - Kelly yellow / greenish-yellow: 0.009
    - Project Blue #1A61FF / Purple #9429EB: 0.032
    - Project Green #29CC38 / Orange #FF7300: 0.032
  - **Protan:**
    - Kelly orange-yellow / yellow-green: 0.004
    - Tableau teal / grey: 0.011
    - Project Red #ED1C2E / Brown #8C4C1F: 0.033
    - Pink / grey pairs are typical
  - **Tritan** (Machado, approximate):
    - Tol bright blue / green: 0.033
    - Tol light light-blue / mint: 0.037
    - Okabe-Ito bluish-green / blue and orange / reddish-purple: ~0.085, so they survive
- **Tactics that worked in the optimisation behind the Q5 palette.** An annealer maximised the worst-case pair over normal, protan, deutan and tritan within named hue bands. It converged on:
  1. **Blue vs purple:** the purple sits at about the same or lower L than blue but redder (h ≈ 303). Alternatively, as in an earlier ΔEOK-only run (Blue L 0.495, Purple L 0.633), purple ~0.14 L lighter than blue at very high chroma. Never a blue-violet at the same L.
  2. **Red vs green vs orange:** red at mid L (0.63), orange lighter (0.74), green at similar L to orange but separated from it on the blue-yellow axis (h 146). Red vs green then relies on ΔL ≈ 0.09 plus residual b.
  3. **Yellow vs lime vs white:** this is the tightest triple on dark backgrounds. Yellow L 0.86, lime L 0.91 shifted strongly green (h 131), white L 0.96 at near-zero chroma.
  4. **Grey placement:** grey sits at mid-high L (0.68) so it does not sit on top of pink or red in lightness.
- **The current project palette** (`C:\GitHub\Magic-Sort-Clone\Assets\HeronCaseRepo\ScriptableObjects\WaterColorPalette.asset`) is very vivid: chroma 0.18–0.26 for most colours. Yet it has three red-green-axis collapses (Red/Brown, Blue/Purple, Green/Orange) at about 0.032 ΔEOK, a fraction of the Okabe-Ito floor.
  - With symbols off, about **6 of its 10 colours** can be used together at a worst-case ≥ 0.08 ΔEOK: Blue, Green, Yellow, Pink, Brown, White.

### Gaps
- Brettel 1997 / Viénot 1999 simulations were **not** run, only Machado. Treat the tritan numbers as indicative only, per DaltonLens.
- No user study was found that measures confusion rates for specific liquid-colour pairs in sort puzzles.

---

## Q3. What are the strongest published categorical palettes, and how many of their colours survive CVD simulation?

### Takeaway
- **Okabe-Ito (7 + black)** is the strongest small set: all 7 chromatic colours stay ≥ 0.076 ΔEOK under every simulated type.
- **Tol's bright, vibrant and muted** sets are explicitly CB-safe but cap at 7–9 colours. Tol says they must not be extended or interpolated.
- **IBM's 5** are vivid and CB-safe, apart from tritan magenta/orange.
- **Tableau 10 and Kelly's 22 are not CVD-safe as complete sets.** Under the strict worst-case ≥ 0.08 ΔEOK (normal, protan, deutan, tritan) test, only 5 of 10 Tableau colours and 8 of 20 Kelly chromatics survive together.
- **No published palette delivers 12 vivid, CVD-safe colours.** 12 colours need either an optimised custom set (Q5) or redundant symbols.

### Cited Findings
- **Okabe-Ito / Wong:** #E69F00 orange, #56B4E9 sky blue, #009E73 bluish green, #F0E442 yellow, #0072B2 blue, #D55E00 vermillion, #CC79A7 reddish purple, plus #000000 black. Also published as the "Wong palette" (Nature Methods 2011). — [NYU Siegal lab](https://siegal.bio.nyu.edu/color-palette/); [R `see::okabeito_colors`](https://search.r-project.org/CRAN/refmans/see/html/okabeito_colors.html)
- **Paul Tol** (technical note issue 3.2, 2021-08-18, cited by khroma; primary site personal.sron.nl was unreachable this session):
  - bright: #4477AA #EE6677 #228833 #CCBB44 #66CCEE #AA3377 #BBBBBB
  - high-contrast: #004488 #DDAA33 #BB5566
  - vibrant: #0077BB #33BBEE #009988 #EE7733 #CC3311 #EE3377 #BBBBBB
  - muted: #CC6677 #332288 #DDCC77 #117733 #88CCEE #882255 #44AA99 #999933 #AA4499 #DDDDDD
  - light: #77AADD #99DDFF #44BB99 #BBCC33 #AAAA00 #EEDD88 #EE8866 #FFAABB #DDDDDD
  - Sources: [tueplots palettes](https://tueplots.readthedocs.io/en/latest/docs_api/tueplots.constants.color.palettes.html); [cb-palettes (PyPI)](https://pypi.org/project/cb-palettes/)
  - khroma says "bright, contrast, vibrant and muted" are colour-blind safe; light is "reasonably distinct"; pale and dark are not very distinct. Max sizes: bright 7, vibrant 7, muted 9, light 9. Qualitative schemes "must be used as given (no interpolation)". — [khroma vignette "Paul Tol's Color Schemes"](https://www.stats.bris.ac.uk/R/web/packages/khroma/vignettes/tol.html)
- **Tableau 10** (current, 2016 redesign) as encoded in d3 `schemeTableau10`: #4E79A7 #F28E2C #E15759 #76B7B2 #59A14F #EDC949 #AF7AA1 #FF9DA7 #9C755F #BAB0AB. — [d3-scale-chromatic Tableau10.js](https://raw.githubusercontent.com/d3/d3-scale-chromatic/main/src/categorical/Tableau10.js)
  - Third-party lists give the orange as #F28E2B; the difference is 1 unit and both are oklch(0.738 0.16 59.3/59.4). — [everycalculators (third-party)](https://everycalculators.com/tableau-select-color-palette-calculated-field.html)
  - Tableau describes the new colours as "softer, more sophisticated, less 'Crayola bright'". — [Tableau blog, "How we designed the new color palettes in Tableau 10"](https://www.tableau.com/blog/colors-upgrade-tableau-10-56782)
- **IBM Design Library CB-safe palette:** #648FFF #785EF0 #DC267F #FE6100 #FFB000. This is secondary sourcing; no IBM-hosted page was found. — [UNC Ainslie lab PDF](https://ainslielab.web.unc.edu/wp-content/uploads/sites/12416/2024/01/Color-palettes-that-can-be-seen-by-the-color-blind.pdf)
- **Kelly's 22 colours of maximum contrast**, in Kelly's order: #F2F3F4 #222222 #F3C300 #875692 #F38400 #A1CAF1 #BE0032 #C2B280 #848482 #008856 #E68FAC #0067A5 #F99379 #604E97 #F6A600 #B3446C #DCD300 #882D17 #8DB600 #654522 #E25822 #2B3D26. — [gist (hex derived from ISCC-NBS centroids, not verified against Kelly 1965)](https://gist.github.com/ollieglass/f6ddd781eeae1d24e391265432297538)
  - Commenters note the gist author "does not endorse" the max-contrast claim, and that orange-yellow, purplish red, yellowish brown and reddish orange "seemed to blend together". — [same gist and search summary](https://gist.github.com/ollieglass/f6ddd781eeae1d24e391265432297538)
  - The order maximises contrast within a set prefix. — [Polychrome docs](https://rdrr.io/pkg/Polychrome/man/palettes.html)
- **Colorgorical** scores palettes on perceptual distance, name difference, name uniqueness and pair preference. — [Brown CS news on Gramazio, Laidlaw & Schloss](https://posts.cs.brown.edu/2016/10/17/connor-gramazio-brings-well-received-information-visualization-software-vis-2016/)
  - Published in IEEE TVCG 23(1), 2017.
  - A 2024 paper notes that perceptual-distance metrics become less useful as differences grow, and that name-based measures help at large differences. — [arXiv 2404.03787](https://arxiv.org/pdf/2404.03787)
- **Trubetskoy's 20-colour list** is ordered so that the first six form a usable set, by frequency on world subway maps. It gives accessibility tiers by population (~95% / ~4% / ~1% / ~0.01%). — [sashamaps.net](https://sashamaps.net/docs/resources/20-colors/)

### Inferences
- **OKLCH for every published hex (computed, L C h):**
  - **Okabe-Ito:**

    | Colour | Hex | L | C | h |
    |---|---|---|---|---|
    | orange | #E69F00 | 0.753 | 0.158 | 76.8 |
    | sky blue | #56B4E9 | 0.735 | 0.117 | 236.2 |
    | bluish green | #009E73 | 0.620 | 0.130 | 165.5 |
    | yellow | #F0E442 | 0.902 | 0.172 | 105.0 |
    | blue | #0072B2 | 0.532 | 0.131 | 244.0 |
    | vermillion | #D55E00 | 0.621 | 0.170 | 47.5 |
    | reddish purple | #CC79A7 | 0.679 | 0.118 | 346.3 |
    | black | #000000 | 0 | 0 | – |

  - **Tol bright:**

    | Hex | L | C | h |
    |---|---|---|---|
    | #4477AA | 0.556 | 0.097 | 250.0 |
    | #EE6677 | 0.683 | 0.168 | 15.1 |
    | #228833 | 0.551 | 0.153 | 145.6 |
    | #CCBB44 | 0.785 | 0.138 | 101.1 |
    | #66CCEE | 0.797 | 0.106 | 222.5 |
    | #AA3377 | 0.519 | 0.169 | 349.4 |
    | #BBBBBB | 0.792 | 0 | – |

  - **Tol vibrant:**

    | Hex | L | C | h |
    |---|---|---|---|
    | #0077BB | 0.549 | 0.137 | 244.6 |
    | #33BBEE | 0.743 | 0.133 | 228.7 |
    | #009988 | 0.612 | 0.110 | 181.3 |
    | #EE7733 | 0.697 | 0.167 | 47.0 |
    | #CC3311 | 0.557 | 0.194 | 33.3 |
    | #EE3377 | 0.632 | 0.223 | 5.2 |
    | #BBBBBB | 0.792 | 0 | – |

  - **Tol muted:**

    | Hex | L | C | h |
    |---|---|---|---|
    | #CC6677 | 0.635 | 0.130 | 11.0 |
    | #332288 | 0.347 | 0.159 | 281.8 |
    | #DDCC77 | 0.841 | 0.108 | 98.3 |
    | #117733 | 0.499 | 0.135 | 148.6 |
    | #88CCEE | 0.812 | 0.084 | 230.8 |
    | #882255 | 0.432 | 0.145 | 354.5 |
    | #44AA99 | 0.674 | 0.098 | 180.4 |
    | #999933 | 0.663 | 0.123 | 109.3 |
    | #AA4499 | 0.553 | 0.167 | 334.6 |
    | #DDDDDD | 0.898 | 0 | – |

  - **Tol light:**

    | Hex | L | C | h |
    |---|---|---|---|
    | #77AADD | 0.722 | 0.092 | 249.2 |
    | #99DDFF | 0.864 | 0.083 | 230.6 |
    | #44BB99 | 0.716 | 0.118 | 170.6 |
    | #BBCC33 | 0.806 | 0.168 | 115.9 |
    | #AAAA00 | 0.714 | 0.156 | 109.8 |
    | #EEDD88 | 0.893 | 0.107 | 98.3 |
    | #EE8866 | 0.730 | 0.134 | 39.1 |
    | #FFAABB | 0.825 | 0.102 | 6.5 |
    | #DDDDDD | 0.898 | 0 | – |

  - **Tableau 10:**

    | Hex | L | C | h |
    |---|---|---|---|
    | #4E79A7 | 0.564 | 0.086 | 251.1 |
    | #F28E2C | 0.738 | 0.159 | 59.3 |
    | #E15759 | 0.638 | 0.173 | 22.5 |
    | #76B7B2 | 0.734 | 0.067 | 189.6 |
    | #59A14F | 0.642 | 0.137 | 141.3 |
    | #EDC949 | 0.845 | 0.147 | 93.0 |
    | #AF7AA1 | 0.646 | 0.084 | 336.6 |
    | #FF9DA7 | 0.798 | 0.117 | 13.5 |
    | #9C755F | 0.595 | 0.059 | 50.6 |
    | #BAB0AB | 0.764 | 0.014 | 48.5 |

  - **IBM:**

    | Hex | L | C | h |
    |---|---|---|---|
    | #648FFF | 0.673 | 0.172 | 266.1 |
    | #785EF0 | 0.587 | 0.210 | 286.2 |
    | #DC267F | 0.595 | 0.222 | 357.7 |
    | #FE6100 | 0.688 | 0.207 | 42.1 |
    | #FFB000 | 0.812 | 0.170 | 76.4 |

  - **Kelly:**

    | Hex | L | C | h |
    |---|---|---|---|
    | #F2F3F4 | 0.964 | 0.002 | – |
    | #222222 | 0.252 | 0 | – |
    | #F3C300 | 0.836 | 0.171 | 90.7 |
    | #875692 | 0.530 | 0.107 | 320.1 |
    | #F38400 | 0.722 | 0.173 | 57.7 |
    | #A1CAF1 | 0.823 | 0.070 | 247.4 |
    | #BE0032 | 0.507 | 0.204 | 19.3 |
    | #C2B280 | 0.765 | 0.069 | 92.4 |
    | #848482 | 0.613 | 0.003 | – |
    | #008856 | 0.552 | 0.127 | 158.9 |
    | #E68FAC | 0.748 | 0.111 | 358.6 |
    | #0067A5 | 0.497 | 0.126 | 245.5 |
    | #F99379 | 0.765 | 0.129 | 35.2 |
    | #604E97 | 0.477 | 0.115 | 292.9 |
    | #F6A600 | 0.784 | 0.166 | 74.4 |
    | #B3446C | 0.548 | 0.149 | 0.9 |
    | #DCD300 | 0.847 | 0.181 | 106.6 |
    | #882D17 | 0.430 | 0.129 | 34.7 |
    | #8DB600 | 0.720 | 0.179 | 124.3 |
    | #654522 | 0.418 | 0.067 | 66.7 |
    | #E25822 | 0.634 | 0.183 | 39.4 |
    | #2B3D26 | 0.337 | 0.046 | 139.0 |

- **CVD survivors.** For each palette: the largest subset whose every pair stays at or above the threshold ΔEOK under normal, protan, deutan and tritan simulation (exhaustive max-clique search). Bracketed is the count when tritan is ignored.

  | Palette | ≥ 0.06 | ≥ 0.08 | ≥ 0.10 |
  |---|---|---|---|
  | Okabe-Ito | **7/7** | 6/7 (drops reddish purple) | 5/7 |
  | Tol bright | 5/7 [N/P/D 7/7] | 5/7 | 4/7 |
  | Tol vibrant | **7/7** | 5/7 | 4/7 |
  | Tol muted | 9/10 | 8/10 | 7/10 |
  | Tol light | 6/9 | 6/9 | 4/9 |
  | Tableau 10 | 5/10 (blue, orange, red, teal, yellow) | 5/10 | 5/10 |
  | IBM | 4/5 [N/P/D 5/5] | 4/5 | 3/5 |
  | Kelly 20 chromatic | 9/20 | 8/20 | 7/20 |
  | Project current | 6/10 [N/P/D 7/10] | 6/10 | 6/10 |

- **Using a data-viz palette in a casual game:**
  - Okabe-Ito and Tol bright/vibrant are already fairly vivid (chroma 0.10–0.22) but mid-lightness (L 0.52–0.90). They are tuned for white paper, not a dark background.
  - Darker members lose contrast on a dark background: Tol muted indigo #332288 (L 0.35) and wine #882255 (L 0.43), Kelly olive-green #2B3D26 (L 0.34).
  - To adapt, keep each colour's **L and hue**, raise **C** toward the sRGB gamut edge for "candy" vibrancy, drop or lift members below L ≈ 0.45, then **re-run the worst-case simulation check.** Raising chroma along the red-green axis does not help colour-blind players.
- **Tol's own limits** (7, 7, 9) and the computed survivor counts both point the same way. **About 7–9 colours is the practical ceiling for colour-only identification across CVD types.** Beyond that, a palette must lean on lightness tiers plus symbols.

### Gaps
- Tableau 20 hex values and Glasbey default-palette hex values were not retrieved from a primary source, so they are not listed.
- Sasha Trubetskoy's 20 hexes were not in the fetched page text.
- The Colorgorical paper's model equations were not retrieved.
- Tol's technical note PDF (primary) was unreachable; hexes come from two independent secondary encodings, which agree.
- IBM hexes are not from an IBM-hosted page.

---

## Q4. What do accessibility guidelines for games (GAG, Xbox XAG, Apple/Google HIG, publisher colour-blind modes) require or recommend for colour-coded gameplay?

### Takeaway
Every major guideline says the same thing. **Never let colour be the only channel for essential information.** Add shape, symbol, pattern or text, and, where colour cannot be avoided, let players choose colours or presets.
- For a colour-sort puzzle, colour *is* the core mechanic. The matching guidance is a **symbol/pattern colour-blind mode** plus a **CVD-robust base palette**. Whole-screen filters are a weaker, last-resort option.

### Cited Findings
- **Game Accessibility Guidelines**, "Ensure no essential information is conveyed by a fixed colour alone" — a **Basic**-level guideline:
  - Pair colour with text, symbol, pattern or shape.
  - Test contrast with simulators.
  - Where colour-only cues are unavoidable, allow player colour choice, ideally deuteranopia/protanopia/tritanopia presets plus custom.
  - Palette-wide filters are "a blunt tool", so try other solutions first.
  - Design for 100% severity.
  - Puzzle examples: **Two Dots** (colourblind mode) and **Puzzle Retreat** (symbols plus colour).
  - Prevalence: red/green difficulty affects "around 8-10% of males". EA Tiburon estimated over half a million colourblind Madden NFL users.
  - — [gameaccessibilityguidelines.com](https://gameaccessibilityguidelines.com/ensure-no-essential-information-is-conveyed-by-a-colour-alone/)
- **Xbox Accessibility Guideline 103** (Additional channels for visual and audio cues; XAG v3.2 dated 8 Jun 2023 per search result):
  - Colour should "Reinforce other channels — never the sole channel".
  - "aim to have **no** cue depend on color alone".
  - "When color reliance is unavoidable, let the player pick the colors used for individual game elements".
  - Critical colour-conveyed content should also use "shape, pattern, icon, or text label".
  - It recommends the Color Oracle simulator.
  - — [Xbox XAG 103](https://devdocs.xbox.com/gaming/accessibility/xbox-accessibility-guidelines/103); [XAG overview](https://learn.microsoft.com/en-us/gaming/accessibility/guidelines)
- **Apple HIG (Accessibility):** "Convey information with more than color alone… Offer visual indicators, like distinct shapes or icons, in addition to color… Consider allowing people to customize color schemes such as chart colors or game characters."
  - Contrast guidance follows WCAG AA: 4.5:1 for text up to 17 pt, 3:1 for 18 pt and above or bold.
  - It asks apps to check contrast in both light and dark appearances.
  - — [Apple HIG Accessibility](https://developer.apple.com/design/human-interface-guidelines/accessibility)
- **Material Design 3:** don't use colour as the only means of conveying information. Use 3:1 for non-text graphics and 4.5:1 for text. **Low confidence:** the fetch returned only a short summary of a JavaScript-rendered page. — [M3 color contrast](https://m3.material.io/foundations/designing/color-contrast)
- **Two Dots** adds a distinct symbol to each dot colour in its colourblind mode, so players can match by shape. The mode is also usable by people with normal colour vision. — [iPhone Life, "App Saturday: Two Dots"](https://iphonelife.com/node/275277); [Oregon State design-class blog](https://blogs.oregonstate.edu/reidfuj/?p=40)
- **Water Sort Quest** (iOS) advertises a red-green colourblind mode. Its listing does not say whether it uses symbols or recolouring. — [App Store, Water Sort Quest](https://apps.apple.com/app/id6462847997)
  - Most water-sort App Store listings found give no accessibility details. — [example listing](https://apps.apple.com/us/app/id1607176949)

### Inferences
- **Compliance pattern for a colour-sort game:**
  1. A CVD-robust default palette (Q5).
  2. An **optional, persistent "Colorblind symbols" toggle.** It draws one high-contrast glyph per liquid colour on each liquid band. Include the pouring stream and the completed-tube cap if they are colour-coded.
  3. Optionally, per-colour user overrides or presets.
  - This meets GAG Basic and XAG 103 "at least two channels", and mirrors Two Dots.
- **Glyphs must be legible at band size.** They need a contrast-safe outline. Use a light glyph with a dark outline, or the reverse depending on the band's L, because liquid L ranges 0.45–0.97.
- **Glyphs must be identifiable by silhouette, not colour.** The set needs 12 distinct shapes: circle, triangle, square, diamond, star, heart, plus, crescent, drop, hexagon, bolt, ring.
- **Puzzle-specific hazards:**
  - The "hidden/unknown liquid" colour must remain distinguishable when symbols are on, for example with a "?" glyph.
  - Colour-only feedback such as a completed tube needs a non-colour cue too. The cap snap already provides a shape cue.

### Gaps
- No primary documentation was retrieved for Ubisoft or EA colour-blind mode implementations (filters vs per-element recolouring), apart from GAG's Madden statistic.
- No 2026 GDC talk specific to colour-sort or match games was found.
- No exact Material 3 text was obtained.
- WCAG SC 1.4.1 "Use of Color" was not fetched this session.

---

## Q5. How should the palette be ordered so early levels use the most distinct colours and later levels add harder ones? (Includes the recommended 12-colour set.)

### Takeaway
- **Order by greedy maximin.** Introduce the next colour that is farthest, by worst-case CVD-simulated distance, from all colours already in play. Late colours are then the ones that create the tightest pairs.
- **Recommended order** for the 12-colour set below, as a nested sequence: **Red, Blue, Yellow, Cyan, White, Pink, Brown, Orange, Purple, Green, Lime, Gray.**
  - The worst-case pair distance stays ≥ 0.18 through 4 colours and ≥ 0.09 through 9.
  - It reaches the set's floor of about 0.076 only at 10+ colours. That floor equals Okabe-Ito's own worst pair.
  - Green is late because it is the hub of three hazard pairs. Moving it earlier is acceptable if symbols are on.

### Cited Findings
- **Prefix ordering in published palettes.** Kelly's list is ordered so that each prefix keeps maximum contrast. — [Polychrome docs](https://rdrr.io/pkg/Polychrome/man/palettes.html)
  - Trubetskoy's list is ordered so that "choosing the first six gives a usable six-color set". — [sashamaps.net](https://sashamaps.net/docs/resources/20-colors/)
- **Glasbey's sequential method** adds one colour at a time, maximising the minimum distance to those already chosen. This is the same greedy-maximin principle. — [Glasbey et al. 2007](https://strathprints.strath.ac.uk/30312/); [glasbey docs](https://glasbey.readthedocs.io/en/latest/)
- **Maximin with CVD simulation in the loop** is the approach palettecore uses: maximise the minimum pairwise distance across normal vision and Machado-simulated protan, deutan and tritan. — [palettecore](https://cran.rstudio.com/web/packages/palettecore/index.html)

### Inferences
- **How the palette was produced.** The approach is design-validated, not user-tested.
  - Simulated annealing ran over OKLCH within named hue bands, with a minimum chroma for "vivid" and 8-bit hex quantisation.
  - It maximised the worst-case min(ΔEOK, ΔE00/100) over normal, protan, deutan and tritan.
  - The 10 existing enum colours (`WaterColor.cs`: Red, Blue, Green, Yellow, Orange, Purple, Cyan, Pink, Brown, White) were held as slots. All 21 pairs of 2 extra slots from {Lime, Sky, Gray, Navy, Teal, Emerald, Maroon} were tried.
- **Choosing the two extras:**
  - Under ΔEOK only, Lime + Gray scored 0.0886, Teal + Maroon 0.0894 and Navy + Maroon 0.0884.
  - Under the stricter combined metric, Teal + Maroon scored 0.081, Gray + Maroon 0.079, Navy + Lime 0.078 and **Lime + Gray 0.076**.
  - **Lime + Gray was chosen.** Maroon and Navy end up at L ≈ 0.40–0.53, which reads poorly on a dark background and sits near the near-black "hidden" liquid. Sky makes three blue-ish liquids, which is a tritan and small-field risk.
- **Recommended 12-colour candidate palette** (sRGB hex; OKLCH computed from the hex; contrast versus the assumed background oklch(0.25 0.04 270)):

  | # intro | Name | Hex | OKLCH (L C h) | WCAG vs bg |
  |---|---|---|---|---|
  | 1 | Red | **#EA464A** | 0.631 0.201 23.8 | 4.19 |
  | 2 | Blue | **#1770FA** | 0.581 0.221 260.0 | 3.62 |
  | 3 | Yellow | **#F1D01B** | 0.860 0.173 97.1 | 10.54 |
  | 4 | Cyan (turquoise) | **#3DCDCF** | 0.776 0.118 196.4 | 8.28 |
  | 5 | White (cream) | **#F6F3E9** | 0.964 0.014 93.0 | 14.47 |
  | 6 | Pink (magenta-pink) | **#C75CAA** | 0.631 0.164 339.0 | 4.24 |
  | 7 | Brown | **#7E462D** | 0.456 0.086 44.2 | 2.14 |
  | 8 | Orange | **#F98A21** | 0.740 0.170 56.7 | 6.64 |
  | 9 | Purple | **#7B4EAE** | 0.519 0.150 303.2 | 2.71 |
  | 10 | Green | **#61BD69** | 0.721 0.148 145.6 | 6.88 |
  | 11 | Lime | **#BEF686** | 0.910 0.153 130.7 | 12.78 |
  | 12 | Gray (warm) | **#9D968E** | 0.677 0.014 71.3 | 5.50 |

- **Per-condition minima for the full 12-colour set:**

  | Condition | min ΔEOK (pair) | min ΔE00 (pair) |
  |---|---|---|
  | Normal | 0.108 (Yellow/Lime) | 16.7 (Green/Lime) |
  | Protan | 0.088 (Orange/Green) | 9.5 (Yellow/Lime) |
  | Deutan | 0.076 (Yellow/Lime) | 8.4 (Red/Green) |
  | Tritan (approximate) | 0.076 (Green/Cyan) | 8.4 (Green/Cyan) |

  - For comparison, the current project palette's minima are 0.032–0.033 ΔEOK (protan and deutan).
- **Hazard pairs.** Combined worst-case below 0.09: Yellow/Lime 0.076, Green/Cyan 0.076, Pink/Gray 0.076, Green/Gray 0.077, Red/Green 0.079, White/Lime 0.079, Orange/Green 0.080, Red/Gray 0.088.
  - Next tier: Blue/Purple 0.091, Purple/Pink 0.091.
  - A level generator can use this list. With symbols off, avoid putting hazard pairs together in low-colour-count levels.
- **Prefix minima for the recommended order** (combined worst-case metric):

  | Colours in play | Prefix minimum |
  |---|---|
  | 2 (Red, Blue) | 0.273 |
  | 3 (+Yellow) | 0.221 |
  | 4 (+Cyan) | 0.184 |
  | 5 (+White) | 0.126 |
  | 6 (+Pink) | 0.112 |
  | 7 (+Brown) | 0.109 |
  | 8 (+Orange) | 0.097 |
  | 9 (+Purple) | 0.091 |
  | 10 (+Green) | 0.076 |
  | 11 (+Lime) | 0.076 |
  | 12 (+Gray) | 0.076 |

  - **Pure greedy from the best pair** would open with Brown and White (0.48) and then go Blue, Orange, Cyan, Red, Pink, Yellow, Purple, Green, Gray, Lime. That opening is less natural for players.
  - **A "familiar-first" alternative** is Red, Blue, Yellow, Green, Orange, Purple, Cyan, Pink, White, Brown, Lime, Gray. It drops to 0.079 at 4 colours, because Red/Green is hazardous. It suits only games that ship with symbols on, or that accept Okabe-Ito-floor separation from level ~4.
- **Rule of thumb for level design:**
  - With 2–5 colours, use only pairs with worst-case ≥ 0.12.
  - With 6–9 colours, stay ≥ 0.09.
  - From 10+ colours, players without colour-vision deficiency still see ≥ 0.108 ΔEOK. The colour-blind floor is about Okabe-Ito's, so prompt the colourblind-symbols option, or default it on, at the level where colour 10 arrives.
- **Implementation caveats:**
  - The values are the *flat* liquid colours. The liquid and glass shaders add gradients and highlights, so validate on captured pixels as well (`Assets/HeronCaseRepo/Art/Shaders/Liquid.shader`, `TubeGlass.shader`).
  - `TubeLiquid.cs` converts palette colours from gamma to linear (comment at about line 330), so hex values in the asset should stay sRGB.
  - Brown (2.1:1) and Purple (2.7:1) have the lowest contrast against a dark background. A thin lighter rim or meniscus highlight helps them read as liquid.
  - P3-capable phones could take extra chroma, but the CVD check must be re-run on any change.

### Gaps
- The palette and ordering are **computed, not user-tested**. Tritan results rely on Machado 2009, which DaltonLens says is inaccurate for tritanopia.
- The real background colour and the shader output of the game were not measured. All contrast numbers assume oklch(0.25 0.04 270).
- The annealer is stochastic, and slightly different runs gave worst-case values of 0.071–0.081 for other slot combinations, so treat the 4th decimal as noise.
- Before shipping:
  - re-check with Brettel/Viénot simulators (for example, Colour Oracle);
  - check on-device at real band size;
  - test with a few colour-blind players, which GAG recommends alongside simulators.
