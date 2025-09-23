#!/bin/bash

echo "Останавливаем контейнеры..."
docker-compose down

echo "Удаляем старые образы..."
docker rmi discountaggregatorbot 2>/dev/null || true

echo "Пересобираем образы..."
docker-compose build --no-cache

echo "Запускаем контейнеры..."
docker-compose up -d

echo "Проверяем логи..."
docker-compose logs -f discountaggregator.bot