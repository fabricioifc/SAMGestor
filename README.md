# SAMGestor - Sistema de Gestão de Retiros

## 📌 Visão Geral

O **SAMGestor** é um sistema completo para gestão de retiros espirituais, cobrindo todo o ciclo de vida — desde a inscrição dos participantes até a alocação em barracas e equipes de serviço.

A aplicação segue uma arquitetura de **microserviços orientada a eventos**, utilizando tecnologias modernas como .NET 8, PostgreSQL e RabbitMQ.

---

## 🚀 Principais Funcionalidades

* **Gestão de Inscrições**
  Registro completo de participantes com validações de negócio

* **Sistema de Contemplação**
  Sorteio aleatório com aplicação de quotas regionais

* **Processamento de Pagamentos**
  Integração com gateway (fake ou Mercado Pago)

* **Geração de Famílias**
  Criação automática de grupos de participantes

* **Gestão de Grupos**
  Notificação via WhatsApp e e-mail

* **Alocação em Barracas**
  Distribuição automática considerando gênero e capacidade

* **Gestão de Serviços**
  Alocação de equipes em espaços específicos do retiro

---

## 🏗️ Padrões Arquiteturais

* **Clean Architecture** → Separação entre domínio, aplicação e infraestrutura
* **CQRS** → Separação de comandos e consultas com MediatR
* **Event-Driven Architecture** → Comunicação assíncrona com RabbitMQ
* **Outbox Pattern** → Garantia de entrega de eventos
* **Repository Pattern** → Abstração de acesso a dados
* **Unit of Work** → Controle de transações

---

## 🧰 Tecnologias Utilizadas

* **.NET 8**
* **PostgreSQL**
* **RabbitMQ**
* **Entity Framework Core**
* **FluentValidation**
* **MediatR**

---

## 📚 Documentação

Para mais detalhes, consulte os documentos abaixo:

* **IMPLEMENTAÇÃO.md**
  Stack tecnológica, microserviços, fluxos de negócio, APIs REST e infraestrutura com Docker

* **FUNCIONALIDADES.md**
  Descrição detalhada das funcionalidades

* **ARQUITETURA.md**
  Decisões arquiteturais e design dos microserviços

* **MODELAGEM.md**
  Modelos de dados e entidades de domínio

---

## ⚙️ Como Executar o Projeto

### 1. Pré-requisitos

Escolha uma das opções:

**Opção A (recomendado):**

* Docker Desktop com integração WSL ativa

**Opção B (ambiente manual):**

* SDK .NET 8
* PostgreSQL 16
* RabbitMQ
* Redis
* MailHog

---

### 2. Configuração do Ambiente

Renomeie o arquivo `.env.sample` para `.env` que está localizado na pasta `infra` e preencha as variáveis de ambiente conforme necessário.

---

### 3. Subir os Serviços

```bash
cd infra
docker compose up -d --build
```

---

### 5. Acessos do Sistema

Após subir os containers:

* **Core API**
  [http://localhost:5000/swagger](http://localhost:5000/swagger)

* **Notification API**
  [http://localhost:5001/swagger](http://localhost:5001/swagger)

* **Payment API**
  [http://localhost:5002/swagger](http://localhost:5002/swagger)

* **RabbitMQ (Painel)**
  [http://localhost:15672](http://localhost:15672)

* **MailHog**
  [http://localhost:8025](http://localhost:8025)

* **pgAdmin**
  [http://localhost:5050](http://localhost:5050)