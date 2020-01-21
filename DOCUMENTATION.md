# Apocryph 
Consensus Network for Autonomous Agents

> Apocryph Agents can automate the cash flow in autonomous organizations, optimize city traffic, or reward the computing power used to train their own neural networks.
## Table of Contents

- [Overview](#overview)
  - [Quick Summary](#quick-summary)
- [How Apocryph works](#how-apocryph-works)
  - [Apocryph Nodes](#apocryph-nodes)
  - [Apocryph Consenus](#apocryph-consenus)
  - [Apocryph Agents](#apocryph-agents)
- [Getting started](#getting-started)
  - [Running via Docker Compose](#running-via-docker-compose)
  - [Running natively](#running-natively)

## Overview

Apocryph is a new consensus network for autonomous agents. From developer perpspective,
we have put a great focus on selecting a tehnology stack comprising widely adopted platforms,
tools and development paradigms.

Below, you can see a short video of how easy is to setup Apocryph test node on you 
local development machine using only Docker and Docker-Compose:

<script id="asciicast-295036" src="https://asciinema.org/a/295036.js" async></script>

### Quick Summary

## How Apocryph works

### Apocryph Nodes

### Apocryph Consenus

### Apocryph Agents

## Getting started

You can run Apocryph on all major operating systems: Windows, Linux and macOS.

### Running via Docker Compose
Using Docker Compose to run Apocryph runtime is the recommended way for users that
would like to run Apocryph validator nodes or developers that will be creating
new Apocryph Agents.

### Prerequisite
- Install [Docker](https://docs.docker.com/install/)
- Install [Docker Compose](https://docs.docker.com/compose/install/)

#### Start IPFS Daemon

Apocryph uses IPFS for its DPoS consensus implementation, thus requires IPFS daemon to run locally on the node:

```bash
docker-compose up -d ipfs
```

#### Start Apocryph Runtime

Before running the Apocryph runtime locally you have to start Perper Fabric in local 
development mode:

- Create Perper Fabric IPC directory  
```bash
mkdir -p /tmp/perper
```
- Run Perper Fabric Docker (This steps require pre-built Perper Fabric image. More information can be found [here](https://github.com/obecto/perper))
```bash
docker-compose up -d perper-fabric
```

Apocryph runtime is implemented as Azure Functions App and can be started with:
```bash
docker-compose up apocryph-runtime
```

### Running natively

In addition to using Docker Compose, you can run Apocryph natively on your machine.
This setup is recommended if you are doing source code contributions to Apocryph Runtime.
The recommended operating system for this setup is Ubuntu 18.04 LTS. 

#### Prerequisite

Before running this sample, you must have the following:

- Install [Azure Functions Core Tools v3](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local#v2)
- Install [.NET Core SDK 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1)
- Install [Docker](https://docs.docker.com/install/)
- Install [IPFS](https://ipfs.io/#install)

#### Enable Perper Functions

Apocryph is based on [Perper](https://github.com/obecto/perper) - stream-based,
horizontally scalable framework for asynchronous data processing. To run Apocryph 
make sure you have cloned Perper repo and have the correct path in Apocryph.proj file.

#### Start IPFS Daemon

Apocryph uses IPFS for its DPoS consensus implementation, thus requires IPFS daemon to run locally on the node:

```bash
ipfs daemon --enable-pubsub-experiment
```

#### Start Apocryph Runtime

Before running the Apocryph runtime locally you have to start Perper Fabric in local 
development mode:

- Building Perper Fabric Docker (in the directory where Perper repo is cloned)
```bash
docker build -t perper/fabric -f docker/Dockerfile .
```
- Create Perper Fabric IPC directory  
```bash
mkdir -p /tmp/perper
```
- Run Perper Fabric Docker 
```bash
docker run -v /tmp/perper:/tmp/perper --network=host --ipc=host -it perper/fabric
```

Apocryph runtime is implemented as Azure Functions App and can be started with:
```bash
func start
```