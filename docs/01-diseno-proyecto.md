# Diseño del proyecto: AulaNube

> Documento que responde, punto por punto, la plantilla de diseño de la actividad (Parte 1, Parte 2 y análisis del equipo).
> Las secciones siguen el orden de la guía. Los textos entre corchetes `[ ]` los completa el equipo.

## Título

**Redes LSTM para la Alerta Temprana de la Deserción Escolar en Colegios sin Infraestructura Tecnológica, a partir de Datos de Matrícula (SIMAT), Notas, Asistencia y Datos Abiertos del MEN y el DANE**

Estructura del título según la guía: **Técnica** (redes LSTM) → **Propósito** (alerta temprana de deserción escolar en colegios sin infraestructura) → **Insumo** (SIMAT, notas, asistencia y datos abiertos del MEN y el DANE).

| Integrantes del grupo | Área disciplinar |
|---|---|
| Santiago [apellidos] | [Profesión / programa] |
| [Integrante 2, si aplica] | [Profesión / programa] |

**Proyecto:** AulaNube, una plataforma en la nube (Azure) con un backend en .NET y un frontend en Angular para que los colegios públicos sin infraestructura TI registren notas y asistencia y reciban alertas tempranas de deserción explicadas.

---

## 1. El problema que quieren resolver

**Ámbitos de la guía que aborda:** transformación digital, brecha tecnológica, toma de decisiones basada en datos, uso de IA en educación y educación.

### Introducción

1. **Contexto.** Los colegios oficiales de Colombia reportan matrícula en el SIMAT y el Ministerio de Educación publica indicadores de deserción, aprobación y repitencia por municipio en el Portal de Datos Abiertos. Cada colegio, además, produce cada periodo notas y registros de asistencia de todos sus estudiantes. Esa información existe, pero está dispersa y llega tarde.
2. **Brecha (problema).** Muchos colegios, sobre todo rurales y de municipios pequeños, no tienen servidores, conectividad estable, software académico ni personal de TI. Las notas y la asistencia viven en planillas de papel o en hojas de cálculo sueltas, así que nadie cruza la caída de notas, las inasistencias y el contexto del estudiante a tiempo. La deserción se descubre cuando el estudiante ya se fue, y el reporte en SIMAT o SIMPADE es retrospectivo.
3. **Propuesta.** AulaNube ofrece, como servicio en la nube y sin infraestructura propia, un registro académico sencillo y un modelo de Deep Learning (LSTM) que analiza la trayectoria de cada estudiante periodo a periodo, calcula su probabilidad de desertar, explica qué factores pesan y sugiere acciones a orientación escolar.

### Problema antecedente

La deserción escolar en básica secundaria y media es multicausal: bajo rendimiento, inasistencia, extraedad y repitencia, trabajo infantil, distancia a la sede y condiciones socioeconómicas. El MEN dispone del sistema SIMPADE para su monitoreo, pero en la práctica los colegios con menos recursos no alimentan ni consultan esa información con la frecuencia necesaria, porque no cuentan con herramientas digitales propias. La evidencia académica sobre analítica del aprendizaje en Colombia (Cardona y Arbeláez, 2019) y sobre estrategias de retención con IA (Alamo, 2024) muestra que la detección temprana basada en datos permite intervenir antes; el reto es llevar esa capacidad a instituciones que no tienen ni la infraestructura ni el personal para construirla.

### Variable objetivo (target)

1. **Variable principal (Y):** `Riesgo_Desercion_Escolar`, problema de **clasificación binaria con salida probabilística**: probabilidad (0 a 1) de que el estudiante abandone el sistema escolar antes de finalizar el año lectivo.
2. **Sub-variables:**
   - `Probabilidad_Desercion`: valor continuo entre 0 y 1 (0 % a 100 %).
   - `Nivel_Alerta`: categoría operativa derivada de umbrales: **Bajo** (< 15 %), **Medio** (15 % a 40 %), **Alto** (≥ 40 %). Los umbrales se fijan para priorizar *recall* (no dejar pasar estudiantes en riesgo) y son ajustables por colegio.
   - `Factores_Explicativos`: contribución de cada variable al puntaje (atribución tipo SHAP / Integrated Gradients).
3. **Mecanismo de cálculo:** la etiqueta histórica se obtiene del estado de matrícula en SIMAT al cierre del año (retirado o desertor vs. matriculado o promovido). El modelo se alimenta con las series por periodo (notas y asistencia) y las variables de contexto del estudiante, y produce la probabilidad; el nivel de alerta se obtiene comparándola con los umbrales.

### Variables independientes

| Dimensión | Variables (X) | Fuente |
|---|---|---|
| **Académica (serie por periodo)** | Promedio de notas, asignaturas perdidas (nota < 3,0), tendencia del promedio entre periodos | Registro de notas en AulaNube |
| **Asistencia (serie por periodo)** | Porcentaje de asistencia, tendencia de la asistencia | Registro de asistencia en AulaNube |
| **Trayectoria escolar** | Extraedad (años por encima de la edad esperada para el grado), repitencia, grado | SIMAT |
| **Socioeconómica** | Estrato, trabaja fuera del horario escolar, beneficiario del PAE | SIMAT, caracterización del colegio |
| **Territorial** | Distancia a la sede, zona urbana o rural, tasa de deserción municipal, índice de pobreza multidimensional | Colegio, MEN (datos.gov.co), DANE |

**Delimitación espacio-temporal:** colegios oficiales de básica secundaria y media (grados 6° a 11°) en municipios con baja infraestructura tecnológica, empezando por un piloto en el Cauca y Antioquia. Ventana histórica de entrenamiento de 3 a 5 años lectivos, con 4 periodos por año. Horizonte de predicción: el cierre del año lectivo en curso, recalculado en cada periodo.

### Pregunta de investigación

**¿En qué medida** una plataforma en la nube (Azure, .NET y Angular) que integra las notas y la asistencia por periodo con datos de SIMAT y datos abiertos del MEN y el DANE, mediante una red LSTM, **permite anticipar** el riesgo de deserción escolar de los estudiantes de colegios sin infraestructura tecnológica propia con al menos un periodo académico de antelación, para **orientar** las acciones de la ruta de permanencia escolar de cada institución?

### ¿A quién impacta?

1. **Impacto directo:** docentes y directores de grupo (registran notas y ven alertas), orientadores escolares y coordinadores (priorizan casos y activan la ruta de permanencia), rectores (tablero de permanencia del colegio) y, sobre todo, los estudiantes en riesgo y sus familias, que reciben acompañamiento a tiempo.
2. **Impacto indirecto:** secretarías de educación certificadas (visión agregada por municipio), el Ministerio de Educación Nacional (información más oportuna para SIMPADE), programas como el PAE y el transporte escolar (focalización) y la comunidad, porque más años de escolaridad mejoran las oportunidades laborales.
3. **Valor agregado:** la nube elimina la barrera de infraestructura, porque el colegio solo necesita un navegador. La red LSTM convierte registros que ya existen (notas y asistencia) en una alerta temprana explicada, de modo que los pocos recursos de orientación se concentran en quienes más lo necesitan.

---

## 2. Arquitectura de IA y toma de decisión tecnológica

### Modelo de Deep Learning

| | Enfoque | Ideal para |
|---|---|---|
| [ ] | CNN (red convolucional) | Imágenes, video, reconocimiento visual |
| **[X]** | **RNN/LSTM (red recurrente)** | **Secuencias, predicción temporal, trayectorias** |
| [ ] | Transformer / LLM | Texto, lenguaje, chatbots |

**¿Por qué LSTM?** La deserción rara vez es un evento súbito: es una **trayectoria** de deterioro (notas que bajan periodo a periodo, inasistencias que aumentan). Una red LSTM procesa la secuencia de periodos y su memoria interna (compuertas de entrada, olvido y salida) captura esa dinámica temporal, algo que un modelo que solo mira el último corte no ve. Las variables de contexto (extraedad, trabajo, distancia, estrato), que no son secuenciales, entran por una rama densa (MLP) y ambas ramas se concatenan antes de la capa de salida sigmoide. La arquitectura está implementada en [`ml/train_lstm.py`](../ml/train_lstm.py).

### Nota metodológica (adaptada a Deep Learning)

1. **Predecir el riesgo de deserción** → aprendizaje **supervisado** con Deep Learning: LSTM + MLP (modelo principal), comparado con una regresión logística como línea base interpretable.
2. **Descubrir perfiles de estudiantes** → aprendizaje **no supervisado**: Autoencoder sobre las variables del estudiante y clustering de los embeddings latentes (perfiles como "rendimiento en caída", "inasistencia por distancia", "extraedad").
3. **Recomendar acciones** → **analítica prescriptiva**: motor de reglas que traduce los factores de mayor contribución en acciones de la ruta de permanencia (nivelación, contacto con la familia, transporte, PAE, modelos flexibles).
4. **Redactar informes** → **IA generativa** (Azure OpenAI) como trabajo futuro, para resumir el estado de permanencia del colegio en lenguaje natural para el consejo directivo.

Las métricas de selección son ROC-AUC, *recall*, precisión y F1 (clasificación), priorizando el *recall* de la clase "deserta". La explicabilidad se obtiene con SHAP / Integrated Gradients.

### Justificación

**¿Qué tipo de datos tienes?**
1. **Tipología y fuentes:** datos estructurados tabulares (SIMAT, caracterización) y **series temporales cortas** (4 periodos por año de notas y asistencia), más indicadores agregados por municipio (MEN en datos.gov.co, DANE).
2. **Dimensiones de entrada:** académica, asistencia, trayectoria escolar, socioeconómica y territorial (tabla de variables independientes).
3. **Mapeo con el modelo:** las series alimentan la rama LSTM; las variables de contexto, la rama MLP; la etiqueta de SIMAT al cierre del año es la Y supervisada. Las mismas variables, sin etiqueta, alimentan el Autoencoder para descubrir perfiles.

**¿Qué problema resuelve este modelo?**
1. **Transformación:** convierte registros administrativos que el colegio ya produce (y que hoy se archivan) en inteligencia para actuar a tiempo.
2. **Capacidades:** predice la probabilidad de deserción, la explica por variable, prioriza la lista de estudiantes, agrupa perfiles de riesgo y sugiere acciones.
3. **Resultado:** más estudiantes atendidos antes de desertar, uso focalizado del tiempo de orientación y reportes más oportunos a la secretaría de educación.

**¿Por qué es la mejor opción?**
1. **Sinergia:** combinar una LSTM (dinámica temporal) con una MLP (contexto) y con modelos no supervisados aprovecha lo mejor de cada paradigma.
2. **Aporte de cada paradigma:** el supervisado da una estimación cuantitativa por estudiante; el no supervisado revela perfiles y anomalías (por ejemplo, un grado completo con caída de asistencia).
3. **Consolidación prescriptiva:** juntas, las dos salidas alimentan el motor de recomendaciones, de modo que el colegio pasa de "saber quién está en riesgo" a "saber qué hacer con cada caso".

### Arquitectura de datos del framework (Deep Learning sobre Azure)

| Fase | Proceso | Fuentes / tecnologías | Salida esperada |
|---|---|---|---|
| 1. Captura | Registro de notas y asistencia en la plataforma; descarga programada de datos abiertos | Angular + API .NET, SIMAT (archivo plano), API Socrata de datos.gov.co, DANE | Datos académicos, de asistencia y territoriales |
| 2. Integración (ETL) | Limpieza, unificación por estudiante y periodo, seudonimización del documento | Azure SQL, Azure Blob Storage, Python (Pandas, PyArrow / Parquet) | Dataset maestro seudonimizado |
| 3. Preprocesamiento profundo | Escalado, imputación, codificación, construcción de secuencias por periodo | NumPy, PyTorch DataLoaders | Tensores `secuencia [n, periodos, 3]` y `contexto [n, 8]` |
| 4. Analítica supervisada | Entrenamiento LSTM + MLP con ponderación de clases | PyTorch en Azure Machine Learning | Modelo candidato |
| 5. Analítica no supervisada | Autoencoder y clustering de embeddings | PyTorch, scikit-learn (k-means, UMAP) | Perfiles de riesgo |
| 6. Selección y ajuste (AutoDL) | Búsqueda de hiperparámetros y validación cruzada; comparación contra la línea base | Optuna, Azure ML sweep jobs | Modelo con mejor ROC-AUC y *recall* |
| 7. Analítica predictiva | Inferencia en cada registro de nota o asistencia | API .NET 8 (ONNX Runtime / JSON de pesos) | Probabilidad y nivel de alerta |
| 8. Analítica prescriptiva | Reglas que mapean factores a acciones de la ruta de permanencia | Servicio de recomendaciones en .NET | Acciones sugeridas por estudiante |
| 9. Visualización | Tablero por colegio y ficha del estudiante | Angular en Azure Static Web Apps | Toma de decisiones del equipo docente |

### Fuentes oficiales integradas

| Entidad | Información | Acceso |
|---|---|---|
| SIMAT (MEN) | Matrícula, estado del estudiante, grado, edad, repitencia | Reporte del colegio / secretaría de educación |
| SIMPADE (MEN) | Variables de riesgo de deserción por estudiante | Secretaría de educación |
| Portal de Datos Abiertos | Estadísticas de educación por municipio: deserción, aprobación, reprobación, repitencia | https://www.datos.gov.co (API Socrata, SoQL) |
| DANE | Proyecciones de población, pobreza multidimensional, ruralidad | https://www.dane.gov.co |
| ICFES | Resultados agregados Saber 11 por institución (contexto de calidad) | https://www.icfes.gov.co |

**Valor agregado de esta arquitectura:** une datos que hoy viven separados (lo que pasa en el aula y lo que dice el contexto del municipio) en una plataforma que el colegio no tiene que instalar ni mantener. Azure aporta la infraestructura, el escalamiento y la seguridad; la red LSTM aporta la lectura temprana de la trayectoria del estudiante.

---

## 3. Los datos que necesitarían

| ¿Qué tipo de datos? | ¿De dónde? | Variable en el dataset maestro |
|---|---|---|
| Notas por asignatura y periodo | Registro en AulaNube (o importación desde Excel) | `promedio_notas`, `asignaturas_perdidas`, `tendencia_notas` |
| Asistencia por periodo | Registro en AulaNube | `asistencia`, `tendencia_asistencia` |
| Matrícula, grado, edad, repitencia, estado al cierre | SIMAT | `grado`, `extraedad`, `repitente`, etiqueta `deserta` |
| Estrato, trabajo, PAE, distancia | SIMAT y caracterización del colegio | `estrato`, `trabaja`, `beneficiario_pae`, `distancia_km` |
| Deserción y aprobación por municipio | datos.gov.co (MEN) mediante API Socrata | `tasa_desercion_municipal` |
| Pobreza y ruralidad | DANE | `ipm_municipal`, `zona_rural` |

**Prototipo:** como los datos reales son de menores de edad y están protegidos (Ley 1581 de 2012), el prototipo usa un dataset **sintético** (3.000 estudiantes, 4 periodos) generado por [`ml/generar_datos_sinteticos.py`](../ml/generar_datos_sinteticos.py), con distribuciones inspiradas en el contexto colombiano.

### Pipeline inteligente

1. **Definición del target:** `Riesgo_Desercion_Escolar` (probabilidad y nivel de alerta).
2. **Ingesta:** formularios de la plataforma (tiempo real), cargas del SIMAT en Blob Storage y consumo de la API Socrata (programado).
3. **Dataset maestro:** unión por estudiante seudonimizado y periodo en Azure SQL; exportación a Parquet para entrenamiento.
4. **Calidad:** validación de rangos (notas 1,0 a 5,0; inasistencias ≤ clases), duplicados, faltantes.
5. **Preprocesamiento:** escalado, codificación, secuencias por periodo.
6. **Tensores y particiones:** entrenamiento, validación y prueba estratificadas; validación cruzada temporal (entrenar con años anteriores y evaluar con el siguiente).
7. **Entrenamiento y búsqueda (AutoDL):** LSTM + MLP, Autoencoder; hiperparámetros con Optuna en Azure ML.
8. **Selección y explicabilidad:** ROC-AUC, *recall*, F1; SHAP / Integrated Gradients; análisis de sesgo por zona y sexo.
9. **Inferencia y prescripción:** la API recalcula la alerta cada vez que se registra una nota o asistencia y propone acciones.
10. **Despliegue:** tablero Angular en Static Web Apps conectado a la API en App Service.

### ETL

- **Extract:** formularios de la plataforma, archivos del SIMAT y API Socrata de datos.gov.co, con Python (Requests, Sodapy, Pandas) en un job programado.
- **Transform:** limpieza de duplicados e inconsistencias, tratamiento de faltantes, integración de fuentes, codificación de categóricas, normalización, ingeniería de características (tendencias, extraedad), detección de atípicos y seudonimización del documento (SHA-256).
- **Load:** tablas en Azure SQL (operación), archivos Parquet en Blob Storage (entrenamiento), artefactos del preprocesamiento y pesos del modelo (`.onnx` / `.json`) versionados en Azure ML y Blob Storage.

### Resultado esperado

Un repositorio analítico seudonimizado en Azure, un modelo de Deep Learning versionado en Azure Machine Learning y una plataforma web (Angular + .NET) que muestra a cada colegio, en cada periodo, qué estudiantes están en riesgo, por qué y qué hacer. El prototipo de este repositorio implementa de punta a punta el flujo con un modelo base interpretable; la LSTM lo reemplaza cuando supere su desempeño.

> **Nota clave:** los datos son el activo estratégico. Su **calidad** (registros completos cada periodo), su **integración** (aula + SIMAT + contexto municipal) y su **actualización automática** determinan la utilidad del modelo más que la complejidad de la red.

### Análisis exploratorio de datos (EDA)

Antes de entrenar: limpieza, tratamiento de faltantes, integración de fuentes, análisis descriptivo, correlaciones, atípicos, **balance de la variable objetivo** (la deserción es minoritaria, por eso se usan pesos de clase), reducción de dimensionalidad y segmentación preliminar. Se revisará la distribución de notas y asistencia por grado y zona, la evolución por periodo de los estudiantes que desertaron frente a los que no y la representatividad de colegios rurales para evitar sesgos.

### Desafíos y matriz DOFA

| Factor | Fortalezas | Oportunidades | Debilidades | Amenazas |
|---|---|---|---|---|
| **Calidad de los datos** | Notas y asistencia se capturan con validación en la propia plataforma | Integrar SIMAT y datos abiertos para enriquecer el modelo | Registros incompletos si los docentes no registran a tiempo | Cambios en formatos del SIMAT o de los datasets abiertos |
| **Acceso** | Solo requiere navegador; nube sin infraestructura local | Conectividad rural creciente y programas de dotación del Estado | Sedes rurales con conectividad intermitente | Cortes de internet; límites de las API públicas |
| **Integración** | API REST e infraestructura como código reproducible | Interoperar con SIMPADE y secretarías de educación | Heterogeneidad entre colegios (escalas, periodos) | Dependencia de un proveedor de nube |
| **Sesgo y ética** | Explicabilidad por variable y supervisión humana | Auditoría algorítmica y monitoreo continuo | Datos históricos pueden reflejar desigualdades (zona, sexo, etnia) | Estigmatizar estudiantes o usar la alerta para excluir |

---

## 4. Objetivos SMART

**Objetivo general:** diseñar e implementar una plataforma en la nube de Azure con backend .NET y frontend Angular que permita a los colegios sin infraestructura tecnológica propia registrar notas y asistencia y obtener alertas tempranas explicadas de deserción escolar mediante un modelo de Deep Learning (LSTM).

**Objetivo general SMART:** durante el semestre académico, construir y desplegar en Azure un prototipo funcional de AulaNube que, con datos sintéticos y luego con datos de un colegio piloto, identifique a los estudiantes en riesgo de deserción con un *recall* mínimo del 80 % y un ROC-AUC mínimo de 0,80, mostrando para cada alerta sus tres factores principales y acciones sugeridas, a un costo de operación inferior a USD 50 mensuales en el ambiente de pruebas.

**Objetivos específicos:**
1. Analizar la situación problema de los colegios sin infraestructura TI y definir variables, fuentes y requisitos éticos (semanas 1 y 2).
2. Diseñar la arquitectura en Azure e implementarla como código con Bicep (semana 3).
3. Desarrollar la API REST en .NET 8 y el frontend Angular con pruebas automatizadas (semanas 3 a 5).
4. Entrenar y comparar el modelo base y la red LSTM, documentando sus métricas (semanas 4 a 6).
5. Desplegar con GitHub Actions, documentar en el README y preparar la presentación (semana 6).

### Parte 1: resumen para el taller

**1. Problema:** los colegios sin infraestructura TI detectan tarde la deserción porque sus datos (notas, asistencia, contexto) están en papel o dispersos; se busca predecir el `Riesgo_Desercion_Escolar` de cada estudiante en cada periodo.

**2. Modelo:** supervisado con LSTM + MLP (trayectoria + contexto) frente a una línea base logística; no supervisado (Autoencoder + clustering) para perfiles; prescriptivo (reglas) para acciones.

**3. Datos:** notas y asistencia por periodo (plataforma), SIMAT, datos abiertos del MEN por municipio y DANE, integrados por ETL en Azure.

**4. Riesgos y mitigación:**

| Riesgo | Mitigación |
|---|---|
| Datos incompletos o tardíos | Validación en los formularios, recordatorios por periodo, importación desde Excel |
| Conectividad intermitente en sedes rurales | Frontend liviano; a futuro, modo sin conexión (PWA) con sincronización |
| Sesgo contra grupos subrepresentados | Métricas por subgrupo (zona, sexo), ponderación de clases, revisión periódica |
| Sobreajuste con pocos datos por colegio | Entrenamiento con datos agregados de varios colegios, validación cruzada temporal |
| Mal uso de la alerta (etiquetar estudiantes) | Supervisión humana obligatoria, mensajes de apoyo y no de sanción, acceso por roles |
| Costos de nube | SQL serverless con autopausa, planes gratuitos o básicos, alertas de presupuesto |

**5. Presentación:** ver la sección "Elevator pitch".

---

## 5. Dimensión ética del proyecto

| Marca | Consideración ética | ¿Cómo lo abordarían? | ¿Aplica? |
|---|---|---|---|
| [X] | **Privacidad y protección de datos** | Los datos son de menores de edad: se aplica la Ley 1581 de 2012 con autorización de padres o acudientes, minimización de datos (solo lo necesario para la alerta), seudonimización del documento, cifrado en tránsito y en reposo en Azure, secretos en Key Vault y acceso por roles con Entra ID. | Sí |
| [X] | **Sesgo en los datos o poblaciones subrepresentadas** | Se miden las métricas por zona (urbana/rural), sexo y pertenencia étnica; si un grupo tiene peor *recall*, se reentrena con ponderaciones o datos adicionales. Variables sensibles como la etnia no se usan para predecir, solo para auditar. | Sí |
| [X] | **Transparencia y explicabilidad** | Cada alerta muestra su probabilidad, sus tres factores principales y la versión del modelo; la documentación explica cómo se calcula. | Sí |
| [X] | **Riesgo de estigmatización** | La alerta es confidencial (solo docentes y orientación del colegio), se presenta como oportunidad de apoyo y nunca se usa para sancionar, excluir o negar cupos. | Sí |
| [X] | **Supervisión humana y toma de decisiones** | El sistema solo sugiere; el comité de convivencia y orientación decide junto con el estudiante y su familia. La interfaz lo recuerda en cada pantalla. | Sí |

---

## Parte 2: Elevator pitch (5 minutos)

**Minuto 0 a 1 | Problema.** En muchos colegios públicos, sobre todo rurales, las notas y la asistencia se llevan en papel. No hay servidores ni personal de sistemas. Cuando alguien nota que un estudiante dejó de venir, ya desertó.

**Minuto 1 a 2 | Solución.** AulaNube es una plataforma en la nube de Azure que el colegio usa desde un navegador, sin instalar nada. Los docentes registran notas y asistencia, y una red neuronal LSTM lee la trayectoria de cada estudiante y avisa quién está en riesgo, por qué y qué hacer.

**Minuto 2 a 3 | Datos.** Usamos lo que el colegio ya produce (notas y asistencia por periodo), el SIMAT y datos abiertos del MEN y el DANE, integrados con un proceso ETL en Azure. Para el prototipo usamos datos sintéticos, porque los datos reales son de menores y están protegidos.

**Minuto 3 a 4 | Ética.** Protegemos a los menores (Ley 1581, seudonimización, acceso por roles), auditamos el sesgo por zona y sexo, explicamos cada alerta y dejamos la decisión siempre en manos del equipo docente y la familia.

**Minuto 4 a 5 | Impacto.** Orientación escolar deja de apagar incendios y empieza a prevenir: concentra su tiempo en los estudiantes que más lo necesitan, un periodo antes. Para el colegio, la tecnología deja de ser una barrera; para el país, cada estudiante que se queda en el aula cuenta.

---

## Análisis del equipo

**¿Cuál es el mayor desafío del proyecto?** Conseguir datos reales, completos y oportunos de colegios con poca conectividad, y lograr que los docentes los registren cada periodo; sin datos de calidad, el mejor modelo no sirve.

**¿Cómo lo mitigarían estratégicamente?** Empezar con un colegio piloto acompañado por la secretaría de educación, ofrecer una interfaz mínima (registrar una nota toma segundos), permitir la importación desde las hojas de cálculo que ya usan, mostrar valor desde el primer periodo con el modelo base y planear un modo sin conexión para sedes rurales.

---

## Bibliografía

- Alamo, E. M.-C. (2024). Análisis de estrategias innovadoras para retención estudiantil con inteligencia artificial: una perspectiva multidisciplinaria. *European Public & Social Innovation Review, 9*, 1-20. https://doi.org/10.31637/epsir-2024-440
- Cardona, J. A. S., y Arbeláez, J. I. T. (2019). Aplicación de Learning Analytics y Educational Data Mining en una institución de educación superior en Colombia. *Revista Ingenierías Universidad de Medellín, 19*(36), 71-89. https://doi.org/10.22395/rium.v19n36a4
- Hochreiter, S., y Schmidhuber, J. (1997). Long short-term memory. *Neural Computation, 9*(8), 1735-1780.
- Lundberg, S. M., y Lee, S.-I. (2017). A unified approach to interpreting model predictions. *Advances in Neural Information Processing Systems, 30*.
- Congreso de Colombia (2012). Ley Estatutaria 1581 de 2012, por la cual se dictan disposiciones generales para la protección de datos personales.
- Ministerio de Educación Nacional. Sistema Integrado de Matrícula (SIMAT) y Sistema de Información para el Monitoreo, Prevención y Análisis de la Deserción Escolar (SIMPADE).
- Microsoft. Documentación de Azure App Service, Azure Static Web Apps, Azure SQL Database serverless y Azure Machine Learning. https://learn.microsoft.com/azure
- Portal de Datos Abiertos de Colombia. https://www.datos.gov.co
