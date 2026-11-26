# XP-25 — Fake Interior

Ticket: https://linear.app/xp-facultad/issue/XP-25/shader-fake-interior-no-funciona-correctamente

El cálculo anterior usaba UV0 sin convertirlas de [0,1] a [-1,1], dejaba una multiplicación con su valor por defecto y conectaba Interior Depth como normal de Sample Reflected Cubemap. La reflexión resultante no era una intersección con una habitación.

El grafo ahora usa FakeInteriorDirection (HLSL): traza un rayo en espacio tangente desde la ventana hasta la primera pared de una caja y entrega esa dirección a Sample Cubemap. Interior Depth escala la profundidad de la caja; el límite mínimo y el divisor seguro mantienen resultados finitos con profundidad cero y componentes paralelas. Rotate About Axis sigue permitiendo orientar el cubemap. El marco ya no emite blanco; se corrigieron los valores fuera de rango del material.

## Verificación en Unity 6000.4.9f1 / HDRP 17.4

Ejecutar **Tools > Fake Interior > Validar XP-25**. La herramienta carga el material usado por la escena y su cubemap Galaxy.psd, fuerza la compilación síncrona, revisa los mensajes del shader y renderiza una ventana en una escena de vista previa independiente. No modifica ni guarda la escena del usuario.

- material--30.png, material-0.png, material-30.png: material real visto a -30°, 0° y 30°.
- depth-0.png, depth-0.25.png, depth-1.png: interior aislado a 25°, con profundidad 0, 0.25 y 1; el cambio de perspectiva se aprecia en paredes y techo.
- validation.txt: resultado del importador, soporte del shader y variación de color en el centro de cada captura. Una imagen uniforme hace fallar la comprobación.

Las capturas se inspeccionaron visualmente después de la ejecución. La prueba comprueba el render en Editor; no sustituye una compilación de Player.
