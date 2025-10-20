# ARG IMAGE_TAG=8
ARG IMAGE_TAG=9
FROM mcr.microsoft.com/dotnet/sdk:${IMAGE_TAG}.0

RUN apt-get update -y && \
    apt-get install python3 python3.11-venv zip unzip -y && \
    python3 -m venv venv && \
    . venv/bin/activate && \
    pip install jprm

RUN mkdir /.dotnet /.nuget /.local && chown 1000:1000 /.dotnet /.nuget /.local

USER 1000:1000