# Pruebas unitarias

Pruebas que verifican de forma aislada componentes o funciones individuales.

**Herramienta:** xUnit. Cubren principalmente la lógica de dominio compartida
(`Pharmacy.Domain` y `Pharmacy.Application`): cálculo de ventas, regla FEFO,
bloqueo de lotes vencidos, reglas de stock y de caja. Objetivo de cobertura:
al menos 70 % en los servicios de negocio críticos (`RNF-007`).
