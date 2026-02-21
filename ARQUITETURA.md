# Arquitetura do Sistema SAMGestor

## Proposta Inicial: Arquitetura Monolítica

Na proposta inicial o sistema SAMGestor foi concebido como uma **arquitetura monolítica** baseada nos princípios de **Clean Architecture**. A aplicação seria estruturada em camadas bem definidas, com separação clara de responsabilidades:

- **Camada de Apresentação (API)**: Controllers REST responsáveis por receber requisições HTTP
- **Camada de Aplicação**: Casos de uso implementados com padrão CQRS usando MediatR
- **Camada de Domínio**: Entidades de negócio, value objects e regras de domínio
- **Camada de Infraestrutura**: Implementações de persistência, mensageria e serviços externos

O sistema utilizaria um **banco de dados centralizado PostgreSQL** contendo todas as entidades do domínio (retiros, inscrições, pagamentos, notificações, famílias, equipes, etc.) em um único schema.

### Diagrama: Arquitetura Monolítica Planejada

```mermaid
graph TB
    subgraph "Aplicação Monolítica SAMGestor"
        API[API Layer<br/>Controllers]
        APP[Application Layer<br/>Handlers CQRS]
        DOM[Domain Layer<br/>Entities & Business Rules]
        INF[Infrastructure Layer<br/>Repositories & Services]
    end

    DB[(PostgreSQL<br/>Banco Centralizado)]
    EXT[Serviços Externos<br/>Email, WhatsApp, Payment Gateway]

    API --> APP
    APP --> DOM
    APP --> INF
    INF --> DB
    INF --> EXT

    style API fill:#e1f5ff
    style APP fill:#fff4e1
    style DOM fill:#ffe1e1
    style INF fill:#e1ffe1
    style DB fill:#f0f0f0
```

## Migração para Microserviços

Durante o desenvolvimento identifiquei a necessidade de migrar para uma **arquitetura de microserviços** pelas seguintes razões:

**Técnicas:**

1. **Separação de Responsabilidades**: Os módulos de pagamento e notificação possuem ciclos de vida e requisitos distintos do core de negócio
2. **Escalabilidade Independente**: O serviço de notificações pode precisar escalar horizontalmente de forma independente durante períodos de alta demanda
3. **Resiliência**: Falhas no serviço de pagamento ou notificação não devem derrubar todo o sistema
4. **Tecnologia Específica**: Cada serviço pode utilizar bibliotecas e otimizações específicas para seu domínio
5. **Desenvolvimento Paralelo**: Equipes podem trabalhar de forma independente em cada serviço

**Negócio:**

1. **Conformidade e Auditoria**: Isolamento do serviço de pagamentos facilita auditorias e conformidade com regulamentações (PCI-DSS)
2. **Manutenibilidade**: Mudanças em regras de notificação não afetam o core de negócio
3. **Testabilidade**: Cada serviço pode ser testado isoladamente
4. **Deploy Independente**: Atualizações em um serviço não requerem deploy de todo o sistema

### Análise Comparativa

| Aspecto | Arquitetura Monolítica | Arquitetura de Microserviços |
|---------|------------------------|------------------------------|
| **Deployment** | Deploy único de toda aplicação | Deploy independente por serviço |
| **Escalabilidade** | Escala vertical (toda aplicação) | Escala horizontal por serviço |
| **Falhas/Resiliência** | Falha em um módulo afeta todo sistema | Falhas isoladas por serviço |
| **Comunicação entre módulos** | Chamadas de método diretas (in-process) | Comunicação assíncrona via eventos (RabbitMQ) |
| **Banco de dados** | Banco centralizado único | Database per Service (3 bancos isolados) |
| **Complexidade de desenvolvimento** | Menor (código em um único projeto) | Maior (múltiplos projetos, contratos compartilhados) |
| **Manutenção** | Mudanças podem ter efeitos colaterais amplos | Mudanças isoladas por bounded context |
| **Independência de equipes** | Equipes trabalham no mesmo código | Equipes podem trabalhar independentemente |

## Arquitetura de Microserviços Implementada

O sistema SAMGestor foi decomposto em **três microserviços principais**, cada um com seu próprio banco de dados e responsabilidades bem definidas:

```mermaid
graph LR
    subgraph "SAM.Core"
        CORE[Core Service<br/>Retiros, Inscrições,<br/>Famílias, Equipes]
        COREDB[(PostgreSQL<br/>core schema)]
    end

    subgraph "SAM.Payment"
        PAY[Payment Service<br/>Processamento<br/>de Pagamentos]
        PAYDB[(PostgreSQL<br/>payment schema)]
    end

    subgraph "SAM.Notification"
        NOT[Notification Service<br/>Envio de<br/>Notificações]
        NOTDB[(PostgreSQL<br/>notification schema)]
    end

    MQ{RabbitMQ<br/>Message Broker}

    CORE <--> MQ
    PAY <--> MQ
    NOT <--> MQ

    CORE --> COREDB
    PAY --> PAYDB
    NOT --> NOTDB

    style CORE fill:#e1f5ff
    style PAY fill:#ffe1e1
    style NOT fill:#e1ffe1
    style MQ fill:#fff4e1
```

### Serviço Core (SAM.Core)

**Responsabilidades:**
- Gerenciamento de retiros espirituais
- Inscrições de participantes
- Gestão de famílias e grupos familiares
- Gestão de equipes de serviço
- Alocação de barracas (tents)
- Controle de vagas e lista de espera
- Autenticação e autorização de usuários
- Geração de relatórios

**Bounded Context:**
O serviço Core representa o **contexto central de negócio** do sistema, responsável por todas as regras relacionadas à organização e gestão de retiros espirituais.

**Principais Entidades de Domínio:**
- `Retreat`: Retiro espiritual com datas, capacidade e configurações
- `Registration`: Inscrição de participante em um retiro
- `Family`: Família/grupo de participantes
- `FamilyMember`: Vínculo entre inscrição e família
- `Team`: Equipe de serviço
- `TeamMember`: Membro de equipe
- `ServiceSpace`: Espaço de serviço disponível
- `ServiceRegistration`: Inscrição para serviço
- `ServiceAssignment`: Alocação de participante em espaço de serviço
- `Tent`: Barraca disponível no retiro
- `TentAssignment`: Alocação de participante em barraca
- `User`: Usuário do sistema
- `WaitingListItem`: Item da lista de espera
- `OutboxMessage`: Mensagens pendentes para publicação

**Banco de Dados (Core DB):**
- Schema: `core`
- Tabelas principais: `retreats`, `registrations`, `families`, `family_members`, `teams`, `team_members`, `service_spaces`, `service_registrations`, `service_assignments`, `tents`, `tent_assignments`, `users`, `waiting_list_items`, `outbox_messages`

### Serviço de Pagamentos (SAM.Payment)

**Responsabilidades:**
- Criação de links de pagamento
- Processamento de confirmações de pagamento
- Integração com gateways de pagamento (Mercado Pago, PIX)
- Rastreamento de status de pagamentos
- Publicação de eventos de pagamento confirmado

**Bounded Context:**
O serviço Payment é responsável por todo o **ciclo de vida financeiro** das inscrições, desde a geração do link de pagamento até a confirmação.

**Fluxo de Processamento de Pagamentos:**
1. Recebe evento `PaymentRequestedV1` do serviço Notification
2. Cria registro de pagamento no banco local
3. Gera link de pagamento 
4. Publica evento `PaymentLinkCreatedV1` via Outbox
5. Recebe confirmação de pagamento (webhook)
6. Atualiza status do pagamento para `Paid`
7. Publica evento `PaymentConfirmedV1` via Outbox

**Principais Entidades:**
- `Payment`: Entidade de pagamento com status, valores, provider e links

**Banco de Dados (Payment DB):**
- Schema: `payment`
- Tabelas principais: `payments`, `outbox_messages`

### Serviço de Notificações (SAM.Notification)

**Responsabilidades:**
- Recepção de eventos de seleção de participantes
- Envio de emails transacionais
- Criação de grupos do WhatsApp para famílias
- Envio de notificações sobre links de pagamento
- Gerenciamento de templates de mensagens
- Orquestração do fluxo: seleção → pagamento → notificação

**Bounded Context:**
O serviço Notification é responsável por toda **comunicação externa** com participantes e pela **orquestração do fluxo de seleção e pagamento**.

**Canais de Notificação Suportados:**
- **Email**: Envio via SMTP para notificações transacionais
- **WhatsApp**: Criação de grupos familiares 

**Principais Entidades:**
- `NotificationMessage`: Mensagem de notificação com canal, destinatário e conteúdo
- `NotificationDispatchLog`: Log de tentativas de envio
- `SelectedRegistration`: Registro de participante selecionado (cache local)

**Banco de Dados (Notification DB):**
- Schema: `notification`
- Tabelas principais: `notification_messages`, `notification_dispatch_logs`, `selected_registrations`

## Comunicação Entre Serviços

### Event-Driven Architecture

O sistema SAMGestor utiliza uma **arquitetura orientada a eventos (Event-Driven Architecture)** para comunicação entre microserviços. Neste modelo:

- **Desacoplamento**: Serviços não conhecem uns aos outros diretamente
- **Assincronicidade**: Comunicação não-bloqueante via mensagens
- **Escalabilidade**: Múltiplos consumidores podem processar eventos em paralelo
- **Auditoria**: Todos os eventos são rastreáveis via `traceId`
- **Resiliência**: Falhas temporárias são tratadas com retry automático

**Padrões Utilizados:**
- **Publisher/Subscriber**: Serviços publicam eventos e outros serviços se inscrevem
- **Event Sourcing Parcial**: Eventos são armazenados antes da publicação (Outbox Pattern)
- **Idempotência**: Consumidores garantem processamento idempotente de eventos

### RabbitMQ como Message Broker

O **RabbitMQ** foi escolhido como message broker pela sua robustez, suporte a padrões avançados de roteamento e facilidade de configuração.

**Configuração do RabbitMQ:**

```mermaid
graph TB
    subgraph "Publishers"
        CORE_PUB[Core Service]
        PAY_PUB[Payment Service]
        NOT_PUB[Notification Service]
    end

    subgraph "RabbitMQ"
        EX[Exchange: sam.topic<br/>Type: Topic]

        Q1[Queue: notification.selection]
        Q2[Queue: notification.serving]
        Q3[Queue: payment.requested]
        Q4[Queue: core.payment.serving]
        Q5[Queue: notification.payment.link]
        Q6[Queue: notification.payment.confirmed]
        Q7[Queue: notification.familygroups.create]
        Q8[Queue: notification.familygroups.notify]
        Q9[Queue: core.familygroups.created]
        Q10[Queue: core.familygroups.failed]
    end

    subgraph "Consumers"
        NOT_CONS[Notification Service]
        PAY_CONS[Payment Service]
        CORE_CONS[Core Service]
    end

    CORE_PUB -->|selection.participant.selected.v1| EX
    CORE_PUB -->|serving.participant.selected.v1| EX
    CORE_PUB -->|family.group.create.requested.v1| EX
    CORE_PUB -->|family.group.notify.requested.v1| EX

    NOT_PUB -->|payment.requested.v1| EX
    NOT_PUB -->|family.group.created.v1| EX
    NOT_PUB -->|family.group.create.failed.v1| EX

    PAY_PUB -->|payment.link.created.v1| EX
    PAY_PUB -->|payment.confirmed.v1| EX

    EX -->|selection.participant.selected.v1| Q1
    EX -->|serving.participant.selected.v1| Q2
    EX -->|payment.requested.v1| Q3
    EX -->|payment.confirmed.v1| Q4
    EX -->|payment.link.created.v1| Q5
    EX -->|payment.confirmed.v1| Q6
    EX -->|family.group.create.requested.v1| Q7
    EX -->|family.group.notify.requested.v1| Q8
    EX -->|family.group.created.v1| Q9
    EX -->|family.group.create.failed.v1| Q10

    Q1 --> NOT_CONS
    Q2 --> NOT_CONS
    Q3 --> PAY_CONS
    Q4 --> CORE_CONS
    Q5 --> NOT_CONS
    Q6 --> NOT_CONS
    Q7 --> NOT_CONS
    Q8 --> NOT_CONS
    Q9 --> CORE_CONS
    Q10 --> CORE_CONS

    style EX fill:#fff4e1
    style CORE_PUB fill:#e1f5ff
    style PAY_PUB fill:#ffe1e1
    style NOT_PUB fill:#e1ffe1
```

**Detalhes da Configuração:**

- **Exchange**: `sam.topic` (tipo Topic, durável)
- **Routing Keys**: Seguem padrão semântico `<dominio>.<entidade>.<acao>.v<versao>`
- **Queues**: Todas são duráveis (sobrevivem a reinicializações)
- **Prefetch**: Configurado para 10 mensagens por consumidor
- **Delivery Mode**: Persistent (mensagens são persistidas em disco)
- **Acknowledgment**: Manual (consumidor confirma após processamento bem-sucedido)

**Configurações de Retry e Dead Letter:**
Atualmente o sistema não implementa Dead Letter Queues (DLQ). Mensagens com falha permanecem na tabela Outbox com `ProcessedAt = null` e são reprocessadas no próximo ciclo do OutboxDispatcher.

### Outbox Pattern Implementado

O **Outbox Pattern** é um padrão de design que garante a **consistência eventual** entre o banco de dados local e o message broker, evitando o problema de "dual write" (escrever em dois sistemas transacionais simultaneamente).

**Diagrama do Fluxo Completo:**

```mermaid
sequenceDiagram
    participant Handler as Command Handler
    participant DB as Database
    participant Outbox as OutboxMessage Table
    participant Worker as OutboxDispatcher<br/>(Background Service)
    participant RMQ as RabbitMQ
    participant Consumer as Event Consumer

    Handler->>DB: 1. Salva entidade de domínio
    Handler->>Outbox: 2. Insere evento na Outbox
    Handler->>DB: 3. COMMIT (transação única)

    Note over DB,Outbox: Garantia de atomicidade:<br/>Ambas as escritas na mesma transação

    Handler->>DB: 4. NOTIFY outbox_new (PostgreSQL)

    loop Polling ou LISTEN/NOTIFY
        Worker->>Outbox: 5. SELECT mensagens não processadas
        Worker->>RMQ: 6. Publica evento no RabbitMQ
        RMQ-->>Worker: 7. ACK de publicação
        Worker->>Outbox: 8. Marca ProcessedAt = NOW()
        Worker->>DB: 9. COMMIT
    end

    RMQ->>Consumer: 10. Entrega evento
    Consumer->>Consumer: 11. Processa evento (idempotente)
    Consumer->>RMQ: 12. ACK manual
```

**Como Funciona o Padrão Outbox:**

1. **Escrita Transacional**: Quando um handler precisa publicar um evento, ele NÃO publica diretamente no RabbitMQ
2. **Persistência Local**: O evento é serializado e inserido na tabela `outbox_messages` na **mesma transação** que altera as entidades de domínio
3. **Atomicidade Garantida**: Se a transação falhar, nem a entidade nem o evento são persistidos
4. **Processamento Assíncrono**: Um background service (`OutboxDispatcher`) lê periodicamente mensagens pendentes
5. **Publicação no Broker**: O dispatcher publica as mensagens no RabbitMQ
6. **Marcação de Processamento**: Após publicação bem-sucedida, a mensagem é marcada com `ProcessedAt`

**Estrutura da Tabela OutboxMessage:**

| Coluna | Tipo | Descrição |
|--------|------|-----------|
| `Id` | UUID | Identificador único da mensagem |
| `Type` | VARCHAR(200) | Routing key do evento (ex: `payment.confirmed.v1`) |
| `Source` | VARCHAR(100) | Serviço de origem (ex: `sam.core`, `sam.payment`) |
| `TraceId` | VARCHAR(100) | ID de rastreamento para correlação |
| `Data` | TEXT/JSONB | Payload completo do evento serializado em JSON |
| `CreatedAt` | TIMESTAMP | Data/hora de criação |
| `ProcessedAt` | TIMESTAMP (nullable) | Data/hora de publicação (NULL = pendente) |
| `Attempts` | INTEGER | Número de tentativas de publicação |
| `LastError` | TEXT (nullable) | Última mensagem de erro (se houver) |

**Como o OutboxDispatcher Funciona:**

**No SAM.Core:**
- **Modo Polling**: Consulta a cada 10 segundos por mensagens pendentes (configurável)
- **Modo LISTEN/NOTIFY**: Utiliza recurso nativo do PostgreSQL para ser notificado instantaneamente quando uma nova mensagem é inserida
- **Batch Size**: Processa até 50 mensagens por ciclo (configurável)
- **Watchdog**: Em modo LISTEN, executa polling a cada 30 segundos como fallback

**No SAM.Payment:**
- **Modo Polling**: Consulta a cada 2 segundos por mensagens pendentes
- **Batch Size**: Processa até 50 mensagens por ciclo

**Como é Garantida a Entrega dos Eventos:**

1. **At-Least-Once Delivery**: Mensagens podem ser reprocessadas em caso de falha
2. **Retry Automático**: Mensagens com falha permanecem com `ProcessedAt = null` e são retentadas
3. **Persistência**: Mensagens são persistidas antes da publicação
4. **Transação Separada**: A marcação de `ProcessedAt` ocorre após confirmação do RabbitMQ

**Como é Garantida a Idempotência:**

1. **Chaves de Idempotência**: Consumidores usam campos como `RegistrationId`, `PaymentId` para detectar duplicatas
2. **Upsert Pattern**: Consumidores fazem `SingleOrDefaultAsync` antes de criar novas entidades
3. **Status Checks**: Verificam status atual antes de aplicar mudanças (ex: não marcar como pago se já está pago)
4. **Correlation IDs**: Uso de `TraceId` para rastreamento e detecção de duplicatas


**Fluxos de Eventos Principais:**

**Fluxo 1: Seleção de Participante para Retiro**
```
Core → selection.participant.selected.v1 → Notification
Notification → payment.requested.v1 → Payment
Payment → payment.link.created.v1 → Notification (envia email com link)
Payment → payment.confirmed.v1 → Core (atualiza status)
Payment → payment.confirmed.v1 → Notification (envia email de confirmação)
```

**Fluxo 2: Seleção de Participante para Serviço**
```
Core → serving.participant.selected.v1 → Notification
Notification → payment.requested.v1 → Payment
Payment → payment.link.created.v1 → Notification (envia email com link)
Payment → payment.confirmed.v1 → Core (atualiza status e auto-aloca em espaço)
```

**Fluxo 3: Criação de Grupo Familiar no WhatsApp**
```
Core → family.group.create.requested.v1 → Notification
Notification → family.group.created.v1 → Core (atualiza link do grupo)
OU
Notification → family.group.create.failed.v1 → Core (marca como falha)
```

## Estratégia de Dados (Database per Service)

O sistema SAMGestor implementa o padrão **Database per Service**, onde cada microserviço possui seu próprio banco de dados isolado. Esta abordagem garante:

- **Autonomia**: Cada serviço controla seu próprio schema
- **Isolamento**: Mudanças no schema de um serviço não afetam outros
- **Escalabilidade**: Bancos podem ser escalados independentemente
- **Tecnologia Específica**: Cada serviço pode usar o SGBD mais adequado (todos usam PostgreSQL atualmente)

### Diagrama: Três Bancos PostgreSQL Isolados

```mermaid
graph TB
    subgraph "PostgreSQL Instance"
        subgraph "Core Database"
            CORE_SCHEMA[Schema: core]
            CORE_TABLES["Tabelas:<br/>• retreats<br/>• registrations<br/>• families<br/>• family_members<br/>• teams<br/>• team_members<br/>• service_spaces<br/>• service_registrations<br/>• service_assignments<br/>• tents<br/>• tent_assignments<br/>• users<br/>• waiting_list_items<br/>• outbox_messages<br/>• change_logs<br/>• region_configs<br/>• blocked_cpfs<br/>• reports<br/>• report_instances<br/>• refresh_tokens<br/>• email_confirmation_tokens<br/>• password_reset_tokens"]
        end

        subgraph "Payment Database"
            PAY_SCHEMA[Schema: payment]
            PAY_TABLES["Tabelas:<br/>• payments<br/>• outbox_messages"]
        end

        subgraph "Notification Database"
            NOT_SCHEMA[Schema: notification]
            NOT_TABLES["Tabelas:<br/>• notification_messages<br/>• notification_dispatch_logs<br/>• selected_registrations"]
        end
    end

    CORE_SERVICE[SAM.Core Service] --> CORE_SCHEMA
    PAY_SERVICE[SAM.Payment Service] --> PAY_SCHEMA
    NOT_SERVICE[SAM.Notification Service] --> NOT_SCHEMA

    CORE_SCHEMA --> CORE_TABLES
    PAY_SCHEMA --> PAY_TABLES
    NOT_SCHEMA --> NOT_TABLES

    style CORE_SCHEMA fill:#e1f5ff
    style PAY_SCHEMA fill:#ffe1e1
    style NOT_SCHEMA fill:#e1ffe1
```

### Como é Mantida a Consistência Entre os Bancos

A consistência entre os bancos de dados é mantida através de **Consistência Eventual** via eventos:

1. **Eventos como Fonte da Verdade**: Mudanças de estado são comunicadas via eventos
2. **Idempotência**: Consumidores processam eventos de forma idempotente
3. **Reconciliação**: Serviços mantêm caches locais (ex: `selected_registrations`) sincronizados via eventos
4. **Sem Transações Distribuídas**: Não há uso de 2PC (Two-Phase Commit)
5. **Compensação**: Em caso de falha, eventos de compensação são publicados (ex: `family.group.create.failed.v1`)

**Exemplo de Fluxo com Consistência Eventual:**

1. Core seleciona participante → publica `selection.participant.selected.v1`
2. Notification recebe evento → salva em `selected_registrations` → publica `payment.requested.v1`
3. Payment recebe evento → cria `payment` → publica `payment.link.created.v1`
4. Payment confirma pagamento → publica `payment.confirmed.v1`
5. Core recebe evento → atualiza `registration` com status pago
6. Notification recebe evento → envia email de confirmação

**Desafios e Soluções:**

| Desafio | Solução Implementada |
|---------|---------------------|
| Duplicação de dados | Cache local mínimo (ex: `selected_registrations`) |
| Sincronização | Eventos com `TraceId` para rastreamento |
| Falhas de comunicação | Outbox Pattern + Retry automático |
| Inconsistências temporárias | Aceitável (consistência eventual) |
| Queries cross-service | Evitadas; cada serviço tem dados necessários |

---


## Esta arquitetura serve como base sólida para futuras expansões do sistema, como:

- Adição de novos canais de notificação (SMS, Push Notifications)
- Integração com múltiplos gateways de pagamento (Stripe, PayPal)
- Implementação de analytics e relatórios avançados
- Criação de serviço de auditoria centralizado
- Implementação de API Gateway para roteamento unificado
- Adição de circuit breakers e políticas de retry avançadas

