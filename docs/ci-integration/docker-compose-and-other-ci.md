---
eyebrow: 'Docs · CI integration'
lede:    'Running the suite outside GitHub Actions — GitLab CI, Jenkins, CircleCI, local dev. The Docker Compose pattern that works anywhere.'

see_also:
  - { href: './github-action.md',                     meta: '5 min' }
  - { href: '../getting-started/installation.md',     meta: '3 min' }
  - { href: '../reference/troubleshooting.md',        meta: '5 min' }

prev: { label: 'GitHub Action',  href: './github-action.md' }
next: { label: 'Basic tests',    href: '../testing-patterns/basic-tests.md' }
---

# Docker Compose and other CI

Outside GitHub Actions, the canonical pattern is **pull the
image, compose up**.

## The CI compose override

The suite ships two compose files:

| File                      | Purpose                                              |
| ------------------------- | ---------------------------------------------------- |
| `docker-compose.yml`      | Local dev — builds from source                       |
| `docker-compose.ci.yml`   | CI override — uses published image, no auto-restart  |

For CI, use both — the override file takes precedence:

<!-- @code-block language="bash" label="terminal — CI start" -->
```bash
export OPCUA_SERVER_IMAGE=ghcr.io/php-opcua/uanetstandard-test-suite:latest

docker compose -f docker-compose.yml -f docker-compose.ci.yml up -d
```
<!-- @endcode-block -->

Without specifying `OPCUA_SERVER_IMAGE`, the override uses
`:latest`. **Pin** to a tag in production CI:

<!-- @code-block language="bash" label="pin the image" -->
```bash
export OPCUA_SERVER_IMAGE=ghcr.io/php-opcua/uanetstandard-test-suite:v1.2.0
```
<!-- @endcode-block -->

## Standard CI sequence

```text
1. Set OPCUA_SERVER_IMAGE.
2. docker pull "$OPCUA_SERVER_IMAGE".
3. docker compose up -d (with the CI override).
4. Wait for ports to be open.
5. Run your tests.
6. docker compose down at the end.
```

### Wait for ports

`docker-compose.ci.yml` disables healthchecks (CI doesn't want
the daemon to restart on failure — it wants to fail loudly).
So your CI step needs to wait for ports itself:

<!-- @code-block language="bash" label="wait loop" -->
```bash
for port in 4840 4841 4842 4843 4844 4845 4846 4847 4848 4849 4851; do
  for i in $(seq 1 60); do
    nc -z localhost "$port" 2>/dev/null && break
    sleep 1
  done
done
```
<!-- @endcode-block -->

Plus a fixed delay for PubSub (no TCP probe — UDP):

<!-- @code-block language="bash" label="wait for pubsub" -->
```bash
sleep 2     # publisher needs a moment to bind its UDP socket
```
<!-- @endcode-block -->

## GitLab CI

<!-- @code-block language="text" label=".gitlab-ci.yml" -->
```text
integration-tests:
  image: docker:24
  services:
    - docker:24-dind
  variables:
    OPCUA_SERVER_IMAGE: ghcr.io/php-opcua/uanetstandard-test-suite:v1.2.0
  before_script:
    - apk add --no-cache docker-cli-compose netcat-openbsd
    - docker pull "$OPCUA_SERVER_IMAGE"
    - docker compose -f docker-compose.yml -f docker-compose.ci.yml up -d
    - |
      for port in 4840 4841 4842 4843 4844 4845 4846 4847 4848 4849 4851; do
        for i in $(seq 1 60); do
          nc -z localhost "$port" 2>/dev/null && break
          sleep 1
        done
      done
  script:
    - export OPCUA_CERTS_DIR=$PWD/certs
    - cargo test
  after_script:
    - docker compose -f docker-compose.yml -f docker-compose.ci.yml down
```
<!-- @endcode-block -->

`docker:dind` is required because the runner needs to launch
Docker.

## Jenkins (declarative pipeline)

<!-- @code-block language="text" label="Jenkinsfile (snippet)" -->
```text
pipeline {
  agent any
  environment {
    OPCUA_SERVER_IMAGE = 'ghcr.io/php-opcua/uanetstandard-test-suite:v1.2.0'
  }
  stages {
    stage('Start servers') {
      steps {
        sh 'docker pull "$OPCUA_SERVER_IMAGE"'
        sh 'docker compose -f docker-compose.yml -f docker-compose.ci.yml up -d'
        sh '''
          for port in 4840 4841 4842 4843 4844 4845 4846 4847 4848 4849 4851; do
            for i in $(seq 1 60); do
              nc -z localhost "$port" 2>/dev/null && break
              sleep 1
            done
          done
        '''
      }
    }
    stage('Test') {
      steps {
        sh 'OPCUA_CERTS_DIR=$PWD/certs cargo test'
      }
    }
  }
  post {
    always {
      sh 'docker compose -f docker-compose.yml -f docker-compose.ci.yml down'
    }
  }
}
```
<!-- @endcode-block -->

## CircleCI

<!-- @code-block language="text" label=".circleci/config.yml (excerpt)" -->
```text
jobs:
  test:
    machine:
      image: ubuntu-2204:current
    environment:
      OPCUA_SERVER_IMAGE: ghcr.io/php-opcua/uanetstandard-test-suite:v1.2.0
    steps:
      - checkout
      - run:
          name: Start OPC UA suite
          command: |
            docker pull "$OPCUA_SERVER_IMAGE"
            docker compose -f docker-compose.yml -f docker-compose.ci.yml up -d
      - run:
          name: Wait for servers
          command: |
            for port in 4840 4841 4842 4843 4844 4845 4846 4847 4848 4849 4851; do
              for i in $(seq 1 60); do
                nc -z localhost "$port" 2>/dev/null && break
                sleep 1
              done
            done
      - run:
          name: Run tests
          command: |
            export OPCUA_CERTS_DIR=$PWD/certs
            cargo test
      - run:
          name: Cleanup
          command: docker compose -f docker-compose.yml -f docker-compose.ci.yml down
```
<!-- @endcode-block -->

## Local development

Outside CI, just use the base compose file (builds from
source):

<!-- @code-block language="bash" label="local" -->
```bash
git clone https://github.com/php-opcua/uanetstandard-test-suite.git
cd uanetstandard-test-suite
docker compose up -d

# Servers are now on localhost:4840-4849, 4851, UDP 14850
# Certs are at ./certs/
```
<!-- @endcode-block -->

To get the **published** image locally (no build):

<!-- @code-block language="bash" label="local with image" -->
```bash
export OPCUA_SERVER_IMAGE=ghcr.io/php-opcua/uanetstandard-test-suite:latest
docker compose -f docker-compose.yml -f docker-compose.ci.yml up -d
```
<!-- @endcode-block -->

## Bitbucket Pipelines, Drone, etc.

The same pattern applies — point at the published image, compose
up, wait for ports, test, compose down. The only platform-specific
parts are:

1. **How to enable Docker** — `services: docker:dind` (Bitbucket
   has its own DinD; Drone uses plugins).
2. **How to expose ports** — Docker-in-Docker on
   shared-host runners typically just works on `localhost`.

## Cleanup hygiene

Always `docker compose down` at the end of the CI job. The
shared runners (Bitbucket, CircleCI, etc.) may not isolate
container state across jobs — leftover containers from one run
can confuse the next.

Add `down` to the `after_script` / equivalent so it runs even
on test failure:

<!-- @code-block language="bash" label="cleanup" -->
```bash
docker compose -f docker-compose.yml -f docker-compose.ci.yml down -v
```
<!-- @endcode-block -->

`-v` removes anonymous volumes, leaving the host filesystem
clean for the next job.

## Image freshness

`ghcr.io/php-opcua/uanetstandard-test-suite` publishes:

- **Versioned tags** — `v1.2.0`, `v1.1.0`, etc. (immutable)
- **`:latest`** — the latest stable release
- **`:master`** — bleeding edge (CI integration testing only)

Production CI should pin a version tag.

## Where to read next

- [Basic tests](../testing-patterns/basic-tests.md) — the first
  recipes you'll run against this setup.
- [Troubleshooting](../reference/troubleshooting.md) — what to do
  when something doesn't work.
