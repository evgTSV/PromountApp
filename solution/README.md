# Документация по PromountApp

---
### Запуск сервисов

---
Для запуска необходимо создать файл .env по шаблону ``.env.example``

Переменные окружения:

| Имя           | Описание                                                          |
|---------------|-------------------------------------------------------------------|
| DB_CONNECTION | Определяет строку подключения, используется синтаксис **ADO.NET** |

### Тестирование

---
#### Unit тестирование

Для запуска юнит тестов используется:
```shell
dotnet test PromountApp/PromountApp.Api.Tests
```

#### Нагрузочное тестирование

Для нагрузочного тестирования используется **JMeter**, сценарии можно найти по пути ``./StressTests/``

### Метрики

---
Для сбора информации и наблюдения за приложением используется OpenTelemetry

Для визуализации данных используется Grafana, по адресу ``http://localhost:3000``

Стандартный пароль от панели Grafana: ``admin``

> **ВНИМАНИЕ**
> 
> Если при просмотре дешбордов отображается ``No data``, то необходимо вручную импортировать дешборды из ``grafana/provisioning/dashboards/``
> В качестве провайдера указать ``promethous``

### Схема отношений моделей СУБД
(для отображения необходима поддержка Mermaid)
```mermaid
erDiagram
    CLIENT {
        Guid client_id
        string login
        int age
        string location
        string gender
    }
    ADVERTISER {
        Guid advertiser_id
        string name
    }
    MLSCORE {
        Guid client_id
        Guid advertiser_id
        int score
    }
    TARGETING {
        Nullable~string~ gender
        Nullable~int~ age_from
        Nullable~int~ age_to
        Nullable~string~ location
    }
    CAMPAIGN {
        Guid campaign_id
        Guid advertiser_id
        int impressions_limit
        int clicks_limit
        float cost_per_impression
        float cost_per_click
        string ad_title
        string ad_text
        int start_date
        int end_date
        string gender
        int age_from
        int age_to
        string location
    }
    
    CLIENT ||--o{ MLSCORE : has
    ADVERTISER ||--o{ MLSCORE : has
    ADVERTISER ||--o{ CAMPAIGN : runs
    CAMPAIGN ||--|{ TARGETING : includes

```