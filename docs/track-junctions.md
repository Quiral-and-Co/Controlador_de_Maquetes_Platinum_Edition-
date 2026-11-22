# Corbes, desviaments i suggeriments de trams buits

## Orientació de corbes
Cada via corba té el paràmetre persistent \`geometry.curveDirection\` (\`right\` o \`left\`). La representació SVG i els ports A/B utilitzen el mateix sentit. La rotació de la peça continua sent independent de la mà de la corba.

## Desviaments
Els desviaments estàndard tenen tres ports:
- A: punta comuna d'unió dels dos itineraris.
- B: sortida de la via recta.
- C: sortida de la via desviada.

El camp «Punt preferit de connexió» controla quin port utilitza la connexió ràpida. La connexió manual continua permetent seleccionar explícitament qualsevol port.

La referència 24624 (DKW / travessa d'unió doble) exposa quatre ports A–D i dibuixa els dos itineraris creuats. El punt preferit es pot escollir entre els quatre. La seva geometria del catàleg continua marcada com no verificada: la representació no certifica l'encaix físic.

## Suggeriments de trams buits
L'eina detecta parelles de ports oberts del mateix nivell, exclou ports ja connectats i cerca combinacions d'una a tres vies rectes i corbes amb geometria confirmada al catàleg C. Per a les corbes es considera el desplaçament real entre els ports A/B i la mà esquerra/dreta. Només proposa combinacions amb diferència màxima de 2 mm respecte del buit. En acceptar-ne una, insereix les peces i crea connexions geomètriques marcades \`verified: false\`. No és una verificació elèctrica ni física.

## Validació
\`npm test\` executa també \`tests/track-junction.cjs\`, que comprova les dues mans de corba, els tres ports del desviament, els quatre ports DKW i la cerca de combinacions de rectes. Qualsevol geometria parcial o no verificada manté el seu estat original.
