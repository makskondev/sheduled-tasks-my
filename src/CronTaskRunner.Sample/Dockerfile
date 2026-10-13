FROM nexus.mcb.ru/repository/docker-infra/dotnet/aspnet:8.0-mkbca

WORKDIR /opt/app
COPY build/libs/ .

RUN chmod -R o+r /opt/app

ENTRYPOINT ["dotnet", "CronTaskRunner.Sample.dll"]
