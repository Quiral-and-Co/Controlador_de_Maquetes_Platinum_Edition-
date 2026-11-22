# Rampes, pendents i hèlix verticals

## Càlcul de rampes rectes
El mòdul `vertical-track-core.js` calcula el pendent a partir de desnivell, longitud i trams de transició. La transició aplica un perfil suau d'entrada i sortida; el pendent màxim es calcula sobre la longitud efectiva, no només dividint l'alçada per la longitud total. Les vies rectes s'escullen automàticament del catàleg Märklin C amb `geometryStatus: confirmed`; el mòdul busca una combinació que s'ajusti a la longitud sol·licitada amb tolerància configurable.

## Hèlix
La hèlix utilitza corbes del catàleg amb geometria confirmada. El radi sol·licitat s'ha d'ajustar a una referència disponible (tolerància de 2 mm); les vies paral·leles són concèntriques i el radi de cada via augmenta segons la separació seleccionada (64 o 77 mm). El nombre de voltes decimal s'accepta només si l'angle total és representable per un nombre enter de peces de la referència seleccionada. El selector de voltes mostra opcions compatibles amb l'angle de la corba.

El pendent es calcula per a cada via amb la seva circumferència real. La cota Z de cada peça segueix un perfil amb transicions d'entrada i sortida.

## Colors i límits
Els colors són una classificació de planificació configurable: verd ≤2%, groc ≤3%, vermell ≤5% i marró fosc >5%. No són límits oficials del fabricant ni substitueixen proves amb el material rodant real. Un resultat marró fosc bloqueja la inserció. El gàlib i les toleràncies físiques s'han de comprovar al disseny de la maqueta abans de construir-la.

## Inserció i seguretat
Les peces s'insereixen al nivell seleccionat amb cotes Z i connexions de model marcades `verified: false`. La inserció no certifica encaix físic, alimentació, gàlib ni seguretat elèctrica. No envia ordres a JMRI, CS2 ni DCC, i no habilita sortides físiques. Les referències amb geometria parcial o no verificada queden excloses dels càlculs automàtics.
