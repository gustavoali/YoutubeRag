# Grafana Dashboards - YoutubeRag.NET

Documentación completa de los dashboards de Grafana para monitoreo del sistema YoutubeRag.NET.

## 📊 Tabla de Contenidos

- [Resumen](#resumen)
- [Configuración](#configuración)
- [Dashboard Principal](#dashboard-principal)
- [Paneles Disponibles](#paneles-disponibles)
- [Alertas y Umbrales](#alertas-y-umbrales)
- [Queries PromQL](#queries-promql)
- [Personalización](#personalización)
- [Troubleshooting](#troubleshooting)

## Resumen

YoutubeRag.NET incluye dashboards pre-configurados de Grafana que se cargan automáticamente al iniciar el stack de monitoreo. Los dashboards proporcionan visualización en tiempo real de:

- Métricas de API y rendimiento HTTP
- Procesamiento de videos y transcripciones
- Uso de infraestructura (DB, caché, storage)
- Background jobs y tareas programadas
- Autenticación y seguridad

## Configuración

### Inicio Rápido

```bash
# Iniciar el stack de monitoreo (Prometheus + Grafana)
docker-compose --profile monitoring up -d

# Acceder a Grafana
# URL: http://localhost:3001
# Usuario: admin
# Contraseña: admin
```

### Auto-provisioning

Los dashboards se configuran automáticamente mediante:

1. **Datasource** (`monitoring/grafana/datasources/prometheus.yml`):
   - Prometheus configurado como datasource por defecto
   - URL: `http://prometheus:9090`
   - Intervalo de consulta: 10s

2. **Dashboards** (`monitoring/grafana/dashboards/dashboards.yml`):
   - Carga automática desde archivos JSON
   - Folder organizacional: "YoutubeRag.NET"
   - Actualización cada 10 segundos

### Configuración Manual

Si necesitas agregar el datasource manualmente:

1. Settings → Data Sources → Add data source
2. Seleccionar "Prometheus"
3. URL: `http://prometheus:9090`
4. Save & Test

## Dashboard Principal

**Nombre**: YoutubeRag.NET Overview
**Ubicación**: `monitoring/grafana/dashboards/youtuberag-overview.json`
**Folder**: YoutubeRag.NET

### Características

- ✅ Auto-refresh cada 10 segundos
- ✅ Time range picker configurable (últimas 6h por defecto)
- ✅ 14 paneles organizados por categoría
- ✅ Thresholds configurados con alertas visuales
- ✅ Tooltips con información detallada
- ✅ Legends con estadísticas (min, max, avg, current)

## Paneles Disponibles

### 1. API Metrics (Fila 1)

#### Panel 1: API Request Rate
- **Tipo**: Time series
- **Métrica**: `rate(http_requests_received_total[1m])`
- **Descripción**: Tasa de requests HTTP por segundo
- **Threshold**: >100 req/s (amarillo), >500 req/s (rojo)

#### Panel 2: API Response Time p95
- **Tipo**: Gauge
- **Métrica**: `histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))`
- **Descripción**: Latencia percentil 95 en milisegundos
- **Threshold**: >500ms (amarillo), >1000ms (rojo)

#### Panel 3: API Error Rate 5xx
- **Tipo**: Gauge (porcentaje)
- **Métrica**: Porcentaje de errores 5xx sobre total de requests
- **Descripción**: Tasa de errores del servidor
- **Threshold**: >1% (amarillo), >5% (rojo)

### 2. Video Processing (Fila 2)

#### Panel 4: Video Processing Rate
- **Tipo**: Time series
- **Métrica**: `rate(youtuberag_videos_processed_total[5m])`
- **Descripción**: Videos procesados por segundo, por status
- **Labels**: status (completed, failed, pending)

#### Panel 5: Storage Usage
- **Tipo**: Gauge (MB)
- **Métrica**: `youtuberag_video_storage_bytes / 1024 / 1024`
- **Descripción**: Almacenamiento utilizado en MB
- **Threshold**: >5000MB (amarillo), >10000MB (rojo)

### 3. Transcription (Fila 3)

#### Panel 6: Transcription Duration p95/p50
- **Tipo**: Time series
- **Métrica**:
  - p95: `histogram_quantile(0.95, rate(youtuberag_transcription_duration_seconds_bucket[5m]))`
  - p50: `histogram_quantile(0.50, rate(youtuberag_transcription_duration_seconds_bucket[5m]))`
- **Descripción**: Latencias de transcripción (mediana y percentil 95)

#### Panel 7: Transcriptions by Language
- **Tipo**: Pie chart
- **Métrica**: `sum by (language) (increase(youtuberag_transcription_duration_seconds_count[1h]))`
- **Descripción**: Distribución de transcripciones por idioma (última hora)

### 4. Search (Fila 4)

#### Panel 8: Search Query Rate
- **Tipo**: Time series
- **Métrica**: `rate(youtuberag_search_queries_total[1m])`
- **Descripción**: Búsquedas por segundo

#### Panel 9: Search Query Latency p99/p50
- **Tipo**: Time series
- **Métrica**:
  - p99: `histogram_quantile(0.99, rate(youtuberag_search_duration_seconds_bucket[5m]))`
  - p50: `histogram_quantile(0.50, rate(youtuberag_search_duration_seconds_bucket[5m]))`
- **Descripción**: Latencias de búsqueda (mediana y percentil 99)

### 5. Background Jobs (Fila 5)

#### Panel 10: Active Background Jobs
- **Tipo**: Gauge
- **Métrica**: `youtuberag_active_jobs_count`
- **Descripción**: Cantidad de jobs en ejecución

#### Panel 11: Background Jobs Execution Rate
- **Tipo**: Time series
- **Métrica**: `rate(youtuberag_background_jobs_executed_total[5m])`
- **Descripción**: Jobs ejecutados por segundo, por tipo y estado

### 6. Infrastructure (Fila 6)

#### Panel 12: Database Connection Pool Usage
- **Tipo**: Gauge (porcentaje)
- **Métrica**: `(youtuberag_db_connections_active / youtuberag_db_connections_total) * 100`
- **Descripción**: Porcentaje de uso del pool de conexiones
- **Threshold**: >70% (amarillo), >90% (rojo)

#### Panel 13: Cache Hit Rate
- **Tipo**: Gauge (porcentaje)
- **Métrica**: `(sum(rate(youtuberag_cache_operations_total{result="hit"}[5m])) / sum(rate(youtuberag_cache_operations_total[5m]))) * 100`
- **Descripción**: Tasa de aciertos de caché
- **Threshold**: <80% (amarillo), <50% (rojo)

### 7. Authentication (Fila 7)

#### Panel 14: Authentication Attempts
- **Tipo**: Time series
- **Métrica**: `rate(youtuberag_authentication_attempts_total[1m])`
- **Descripción**: Intentos de autenticación por segundo, por resultado (success, failed)

## Alertas y Umbrales

### Thresholds Configurados

| Panel | Amarillo (Warning) | Rojo (Critical) |
|-------|-------------------|-----------------|
| API Request Rate | >100 req/s | >500 req/s |
| Response Time p95 | >500ms | >1000ms |
| Error Rate 5xx | >1% | >5% |
| Storage Usage | >5GB | >10GB |
| DB Pool Usage | >70% | >90% |
| Cache Hit Rate | <80% | <50% |

### Configuración de Alertas

Para configurar alertas basadas en los paneles:

1. Abrir panel → Edit
2. Alert tab → Create alert rule
3. Configurar condiciones (ej: Response Time > 1000ms por 5 minutos)
4. Configurar notificaciones (email, Slack, etc.)

**Ejemplo de alerta recomendada**:

```yaml
# Alert: High API Response Time
- alert: HighAPILatency
  expr: histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m])) > 1
  for: 5m
  labels:
    severity: critical
  annotations:
    summary: "API response time is too high"
    description: "95th percentile latency is {{ $value }}s (threshold: 1s)"
```

## Queries PromQL

### Queries Útiles

#### Rate de requests por endpoint
```promql
sum by (endpoint) (rate(http_requests_received_total[5m]))
```

#### Top 5 endpoints más lentos
```promql
topk(5,
  histogram_quantile(0.95,
    sum by (endpoint, le) (rate(http_request_duration_seconds_bucket[5m]))
  )
)
```

#### Videos procesados en la última hora
```promql
sum(increase(youtuberag_videos_processed_total{status="completed"}[1h]))
```

#### Tiempo promedio de transcripción por modelo
```promql
sum by (model) (rate(youtuberag_transcription_duration_seconds_sum[5m]))
/
sum by (model) (rate(youtuberag_transcription_duration_seconds_count[5m]))
```

#### Tasa de errores de background jobs
```promql
sum(rate(youtuberag_background_jobs_executed_total{status="failed"}[5m]))
/
sum(rate(youtuberag_background_jobs_executed_total[5m]))
```

## Personalización

### Crear Dashboard Personalizado

1. **Duplicar dashboard existente**:
   - Dashboard → Settings → Save As
   - Renombrar y modificar

2. **Crear desde cero**:
   - + → Create → Dashboard
   - Add panel → Seleccionar visualización
   - Configurar query PromQL

### Agregar Panel Nuevo

```json
{
  "title": "Mi Panel Personalizado",
  "type": "timeseries",
  "targets": [
    {
      "expr": "tu_query_promql_aqui",
      "legendFormat": "{{label_name}}"
    }
  ],
  "fieldConfig": {
    "defaults": {
      "unit": "reqps",
      "thresholds": {
        "steps": [
          { "color": "green", "value": null },
          { "color": "yellow", "value": 50 },
          { "color": "red", "value": 100 }
        ]
      }
    }
  }
}
```

### Variables de Dashboard

El dashboard usa estas variables:

- **`$datasource`**: Prometheus datasource seleccionado
- **`$interval`**: Intervalo de consulta automático

Para agregar variables:
1. Dashboard → Settings → Variables
2. Add variable
3. Configurar query, label, etc.

## Troubleshooting

### Dashboard no se carga automáticamente

**Problema**: Dashboard no aparece después de iniciar Grafana

**Solución**:
```bash
# Verificar que el archivo existe
ls monitoring/grafana/dashboards/youtuberag-overview.json

# Verificar logs de Grafana
docker-compose logs grafana | grep -i provision

# Reiniciar Grafana
docker-compose restart grafana
```

### "No data" en paneles

**Problema**: Paneles muestran "No data"

**Soluciones**:

1. **Verificar Prometheus**:
   ```bash
   # Verificar que Prometheus está scraping
   curl http://localhost:9090/api/v1/targets
   ```

2. **Verificar métricas disponibles**:
   ```bash
   # Ver métricas en Prometheus
   curl http://localhost:9090/api/v1/label/__name__/values | grep youtuberag
   ```

3. **Verificar endpoint /metrics**:
   ```bash
   # Ver métricas de la API
   curl http://localhost:5000/metrics
   ```

4. **Ajustar time range**: Cambiar el rango de tiempo en el dashboard (ej: últimas 5 minutos)

### Queries lentas

**Problema**: Dashboard tarda mucho en cargar

**Soluciones**:

1. **Aumentar step interval**:
   - Edit panel → Query options → Min step → 30s

2. **Reducir time range**:
   - Time picker → Last 1 hour (en lugar de 6 hours)

3. **Optimizar queries**:
   ```promql
   # En lugar de:
   rate(metric[5m])

   # Usar:
   rate(metric[5m:30s])  # step de 30s
   ```

### Thresholds no se muestran

**Problema**: Colores de alerta no aparecen

**Solución**:
1. Edit panel → Field → Thresholds
2. Verificar que "Color scheme" está en "From thresholds"
3. Verificar unidades (ej: si threshold es 0.5 pero valor es 500ms)

### Conexión a Prometheus falla

**Problema**: "Cannot connect to Prometheus"

**Solución**:
```bash
# Verificar que Prometheus está running
docker-compose ps prometheus

# Verificar URL del datasource
# Debe ser: http://prometheus:9090 (nombre del servicio Docker)
# NO: http://localhost:9090

# Recrear stack
docker-compose --profile monitoring down
docker-compose --profile monitoring up -d
```

## Referencias

- [Grafana Dashboard Documentation](https://grafana.com/docs/grafana/latest/dashboards/)
- [PromQL Basics](https://prometheus.io/docs/prometheus/latest/querying/basics/)
- [Prometheus Best Practices](https://prometheus.io/docs/practices/naming/)
- [YoutubeRag Prometheus Metrics](./PROMETHEUS_METRICS.md)

---

**Última actualización**: 2025-10-11
**Autor**: YoutubeRag.NET Team
