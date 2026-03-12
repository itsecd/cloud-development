SHELL := /bin/bash

DOTNET8_PREFIX ?= /opt/homebrew/opt/dotnet@8
DOTNET8_BIN := $(DOTNET8_PREFIX)/bin
DOTNET8_ROOT := $(DOTNET8_PREFIX)/libexec

APPHOST_PROJECT := CloudDevelopment.AppHost/CloudDevelopment.AppHost.csproj
API_PROJECT := CourseGenerator.Api/CourseGenerator.Api.csproj
REDIS_CONTAINER := lab1-redis
REDIS_IMAGE := redis:7-alpine

.PHONY: help redis-up redis-down restore build run-apphost run-api api-check

help:
	@echo "Targets:"
	@echo "  make redis-up      - start Redis container"
	@echo "  make redis-down    - stop Redis container"
	@echo "  make restore       - restore workloads and NuGet packages"
	@echo "  make build         - build solution in Debug"
	@echo "  make run-apphost   - run Aspire AppHost"
	@echo "  make run-api       - run API standalone on http://localhost:5117"
	@echo "  make api-check     - call API endpoint (standalone mode)"

redis-up:
	@docker ps -a --format '{{.Names}}' | grep -qx '$(REDIS_CONTAINER)' \
		&& docker start $(REDIS_CONTAINER) \
		|| docker run -d --name $(REDIS_CONTAINER) -p 6379:6379 $(REDIS_IMAGE)

redis-down:
	@docker stop $(REDIS_CONTAINER)

restore:
	@export PATH="$(DOTNET8_BIN):$$PATH"; \
	export DOTNET_ROOT="$(DOTNET8_ROOT)"; \
	dotnet workload restore $(APPHOST_PROJECT); \
	dotnet restore CloudDevelopment.sln

build:
	@export PATH="$(DOTNET8_BIN):$$PATH"; \
	export DOTNET_ROOT="$(DOTNET8_ROOT)"; \
	dotnet build CloudDevelopment.sln -c Debug

run-apphost:
	@export PATH="$(DOTNET8_BIN):$$PATH"; \
	export DOTNET_ROOT="$(DOTNET8_ROOT)"; \
	dotnet run --project $(APPHOST_PROJECT)

run-api:
	@export PATH="$(DOTNET8_BIN):$$PATH"; \
	export DOTNET_ROOT="$(DOTNET8_ROOT)"; \
	ASPNETCORE_ENVIRONMENT=Development dotnet run --project $(API_PROJECT) --urls http://localhost:5117

api-check:
	@curl "http://localhost:5117/api/courses/generate?count=2"
