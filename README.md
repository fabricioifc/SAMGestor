# SAMGestor - Sistema de Gestão de Retiros

## Visão Geral do Sistema

O **SAMGestor** é um sistema completo de gestão de retiros espirituais que gerencia todo o ciclo de vida de um retiro, desde a inscrição dos participantes até a alocação em barracas e serviços. O sistema é construído com arquitetura de microserviços orientada a eventos, utilizando .NET 8, PostgreSQL e RabbitMQ.

### Principais Funcionalidades

- **Gestão de Inscrições**: Registro completo de participantes com validações de negócio
- **Sistema de Contemplação**: Sorteio aleatório com quotas regionais
- **Processamento de Pagamentos**: Integração com gateway de pagamento (fake/MercadoPago)
- **Geração de Famílias**: Criação automática de grupos 
- **Gestão de Grupos**: Criação e notificação de grupos de WhatsApp/Email
- **Alocação em Barracas**: Distribuição automática por gênero e capacidade
- **Gestão de Serviços**: Alocação de equipe de serviço em espaços específicos

### Padrões Arquiteturais

- **Clean Architecture**: Separação clara entre domínio, aplicação e infraestrutura
- **CQRS**: Separação de comandos e consultas usando MediatR
- **Event-Driven Architecture**: Comunicação assíncrona via RabbitMQ
- **Outbox Pattern**: Garantia de entrega de eventos com transações
- **Repository Pattern**: Abstração de acesso a dados
- **Unit of Work**: Gerenciamento de transações

### Tecnologias Principais

- **.NET 8**: Framework principal
- **PostgreSQL**: Banco de dados relacional
- **RabbitMQ**: Message broker para eventos
- **Entity Framework Core**: ORM
- **FluentValidation**: Validação de comandos
- **MediatR**: Mediador para CQRS

## Documentação Completa

Para informações detalhadas sobre o sistema documentos:

- **[IMPLEMENTAÇÃO](./IMPLEMENTACAO.md)** - Stack tecnológica, estrutura de microserviços, camadas da arquitetura, fluxos de negócio, APIs RESTful, integração com serviços externos e infraestrutura com Docker
- **[FUNCIONALIDADES](./FUNCIONALIDADES.md)** - Descrição detalhada de todas as funcionalidades do sistema
- **[ARQUITETURA](./ARQUITETURA.md)** - Padrões arquiteturais, design de microserviços e decisões técnicas
- **[MODELAGEM](./MODELAGEM.md)** - Modelos de dados, entidades de domínio e relacionamentos


