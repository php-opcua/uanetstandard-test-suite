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
| `docker-compose.yml`      | Always builds the image locally (`build: .`)         |
| `docker-compose.ci.yml`   | Minimal CI override — sets `restart: "no"` on every service and disables the single healthcheck on `opcua-no-security` |

The CI override is intentionally tiny. It does **not** swap the
image (there is no `image:` field anywhere in `docker-compose.ci.yml`),
it does **not** read `OPCUA_SERVER_IMAGE` (no such variable
exists in the shipped compose files), and it does **not**
configure any registry pull. Both files build from local source.

For CI, layer the override on top of the base file so containers
do not auto-restart on crash:

<!-- @code-block language="bash" label="terminal — CI start" -->
```bash
docker compose -f docker-compose.yml -f docker-compose.ci.yml up -d --build --wait
```
<!-- @endcode-block -->

If your CI runner already has the suite repo (e.g. via `actions/checkout`),
`docker compose up --build` rebuilds the image on the runner. The
build context is the repo root — no external image registry is
consulted by the shipped configuration.

## Standard CI sequence

```text
1. Check out (or copy) the uanetstandard-test-suite repo.
2. docker compose -f docker-compose.yml -f docker-compose.ci.yml up -d --build --wait
3. (Optional) Wait for ports / poll `--health` on the services
   you care about.
4. Run your tests.
5. docker compose -f docker-compose.yml -f docker-compose.ci.yml down at the end.
```

### Wait for ports

`docker-compose.ci.yml` disables the healthcheck on the
`opcua-no-security` service only — that is the only service in
`docker-compose.yml` that declares one in the first place. The
other 11 services have no healthcheck either way; `docker compose
up --wait` cannot block on their readiness. So CI typically polls
the TCP ports itself:

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
  before_script:
    - apk add --no-cache docker-cli-compose git netcat-openbsd
    - git clone --depth 1 https://github.com/php-opcua/uanetstandard-test-suite.git
    - cd uanetstandard-test-suite
    - docker compose -f docker-compose.yml -f docker-compose.ci.yml up -d --build
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
    - cd uanetstandard-test-suite && docker compose -f docker-compose.yml -f docker-compose.ci.yml down
```
<!-- @endcode-block -->

`docker:dind` is required because the runner needs to launch
Docker.

## Jenkins (declarative pipeline)

<!-- @code-block language="text" label="Jenkinsfile (snippet)" -->
```text
pipeline {
  agent any
  stages {
    stage('Start servers') {
      steps {
        sh 'git clone --depth 1 https://github.com/php-opcua/uanetstandard-test-suite.git'
        sh 'cd uanetstandard-test-suite && docker compose -f docker-compose.yml -f docker-compose.ci.yml up -d --build'
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
        sh 'OPCUA_CERTS_DIR=$PWD/uanetstandard-test-suite/certs cargo test'
      }
    }
  }
  post {
    always {
      sh 'cd uanetstandard-test-suite && docker compose -f docker-compose.yml -f docker-compose.ci.yml down'
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
    steps:
      - checkout
      - run:
          name: Start OPC UA suite
          command: |
            git clone --depth 1 https://github.com/php-opcua/uanetstandard-test-suite.git
            cd uanetstandard-test-suite
            docker compose -f docker-compose.yml -f docker-compose.ci.yml up -d --build
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
            export OPCUA_CERTS_DIR=$PWD/uanetstandard-test-suite/certs
            cargo test
      - run:
          name: Cleanup
          command: cd uanetstandard-test-suite && docker compose -f docker-compose.yml -f docker-compose.ci.yml down
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

To run with the CI override locally (containers will not
auto-restart on crash, easier to inspect failures):

<!-- @code-block language="bash" label="local with CI override" -->
```bash
docker compose -f docker-compose.yml -f docker-compose.ci.yml up -d --build
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

The shipped compose files build the image from the repo's
`Dockerfile` every time (`build: .`). To pin a stable version,
check out a specific git tag of `php-opcua/uanetstandard-test-suite`
before running `docker compose up --build`. A pre-built registry
image is not configured by the shipped files; if your CI maintains
one, you can layer a third compose file that supplies an
`image:` field per service.

## Where to read next

- [Basic tests](../testing-patterns/basic-tests.md) — the first
  recipes you'll run against this setup.
- [Troubleshooting](../reference/troubleshooting.md) — what to do
  when something doesn't work.
