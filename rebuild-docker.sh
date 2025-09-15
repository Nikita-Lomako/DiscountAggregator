#!/bin/bash


echo "Останавливаем существующие контейнеры..."
docker-compose down

echo "Удаляем старые образы..."
docker rmi discountaggregator.bot:latest 2>/dev/null || true

echo "Очищаем кэш Docker..."
docker system prune -f

echo "Пересобираем контейнеры..."
docker-compose build --no-cache

echo "Запускаем контейнеры..."
docker-compose up -d

echo "Проверяем логи..."
docker-compose logs -f discountaggregator.bot