# Biblioteca de catàleg Märklin C (H0) · editor 0.9

La biblioteca és una base de dades JSON independent del motor de dibuix: `catalogue-marklin-c.json`. Per afegir una referència, s'hi incorpora un objecte amb `ref`, `name`, `category`, `type` i, quan es coneixen, `lengthMm`, `radiusMm` i `angleDeg`. `geometryStatus` pot ser `confirmed`, `partial` o `unverified`.

## Referències incorporades

- Rectes: 24064, 24071, 24077, 24094, 24172, 24188, 24229, 24236, 24360.
- Corbes: 24107, 24115, 24130 (R1); 24207, 24215, 24230 (R2); 24315, 24330 (R3); 24430 (R4); 24530 (R5).
- Desviaments i creuaments: 24611, 24612, 24624, 24630, 24649, 24671, 24672.
- Complements, accessoris i transicions: 24001, 24088, 24194, 24206, 24224, 24294, 24912, 24922, 24951, 24977, 24978, 24994, 24995, 24997, 20997, 74861, 74862, 74920, 74924.
- Conjunts comercials: 20099, 20299. Són paquets de diverses peces, no geometries d’una peça única.
- Referències Start up i variants transparents: 20130, 20172, 20188, 20196, 20197, 20199, 20224, 20230, 20330, 20360, 20611, 20612, 20672, 20977.
- Desviaments esvelts, creuaments i accessoris: 24640, 24711, 24712, 24720, 24740, 24771, 24772, 74861, 74862, 74920, 74924.

Les dimensions de rectes i corbes indicades com a confirmades provenen de llistes de peces i geometria publicades per Märklin. Les desviacions i creuaments amb geometria parcial o no verificada es dibuixen de manera esquemàtica. Els seus ports no s'han de considerar topològicament exactes ni utilitzar per fabricar plantilles o validar encaixos.

## Cobertura i ampliació

Aquesta primera càrrega conté referències identificades en documentació pública oficial, però no es declara com a transcripció exhaustiva de totes les referències històriques, retirades, de catàleg actual ni accessoris del C-Gleis. La biblioteca és ampliable sense tocar `app.js`; la següent fase haurà de completar-la contra el catàleg oficial vigent i les fitxes tècniques individuals.

## Fonts documentals

- Märklin, pàgina oficial de productes C-Gleis (68 productes): https://www.marklin.com/products/gauge-h0/gleismaterial/c-gleis
- Märklin, *C-Gleispläne*, PDF oficial: https://static.maerklin.de/damcontent/61/a2/61a276495a4cc96ef1a036e8826a389e1768493727.pdf
- Märklin Magazin, *Gleisplanbuch*, llista de peces i dimensions: https://static.maerklin.de/damcontent/df/bc/dfbce9e4e5a0878f563bead482e39d4d1675430400.pdf

No s'han copiat imatges, dibuixos CAD, símbols gràfics ni recursos propietaris. Les representacions són esquemes originals del projecte.
