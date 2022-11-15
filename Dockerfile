FROM mcr.microsoft.com/dotnet/sdk:6.0.301-bullseye-slim AS build

RUN apt-get update

#---------------------------------------------------------------------
# Tools setup
#---------------------------------------------------------------------
RUN dotnet tool install --global dotnet-ef
RUN dotnet tool install --global coverlet.console
RUN dotnet tool install --global dotnet-reportgenerator-globaltool

# sonarcloud
ARG SONAR_PROJECT_KEY=Tech-Fabric_CloudFabric.EAV
ARG SONAR_OGRANIZAION_KEY=tech-fabric
ARG SONAR_HOST_URL=https://sonarcloud.io
ARG SONAR_TOKEN
ARG GITHUB_TOKEN
RUN dotnet tool install --global dotnet-sonarscanner
RUN apt-get update && apt-get install -y openjdk-11-jdk
#sonarcloud

ENV PATH="/root/.dotnet/tools:${PATH}" 

RUN curl -L https://raw.githubusercontent.com/Microsoft/artifacts-credprovider/master/helpers/installcredprovider.sh | sh
#---------------------------------------------------------------------
# /Tools setup
#---------------------------------------------------------------------


#---------------------------------------------------------------------
# Test database setup
#---------------------------------------------------------------------
RUN apt-get install -y postgresql postgresql-client postgresql-contrib

USER postgres

RUN echo "local   all   all               md5" >> /etc/postgresql/13/main/pg_hba.conf &&\
    echo "host    all   all   0.0.0.0/0   md5" >> /etc/postgresql/13/main/pg_hba.conf

RUN echo "listen_addresses='*'" >> /etc/postgresql/13/main/postgresql.conf
RUN service postgresql start \
    && psql --command "CREATE ROLE cloudfabric_eventsourcing_test WITH CREATEDB NOSUPERUSER NOCREATEROLE INHERIT NOREPLICATION CONNECTION LIMIT -1 LOGIN PASSWORD 'cloudfabric_eventsourcing_test';" \
    && psql --command "DROP DATABASE IF EXISTS cloudfabric_eventsourcing_test;" \
    && psql --command "CREATE DATABASE cloudfabric_eventsourcing_test WITH OWNER = cloudfabric_eventsourcing_test ENCODING = 'UTF8' CONNECTION LIMIT = -1;" \
    && psql --command "GRANT ALL ON DATABASE cloudfabric_eventsourcing_test TO postgres;"
#---------------------------------------------------------------------
# /Test database setup
#---------------------------------------------------------------------


USER root
WORKDIR /

#---------------------------------------------------------------------
# Nuget restore 
# !Important: this is a nice hack to avoid package restoration on each docker build step.
# Since we only copy nuget.config and csproj files, docker will not run restore unless nuget.config or csproj files change.
#---------------------------------------------------------------------
#COPY nuget.config /src/nuget.config

COPY CloudFabric.EAV.Domain/CloudFabric.EAV.Domain.csproj /src/CloudFabric.EAV.Domain/CloudFabric.EAV.Domain.csproj
COPY CloudFabric.EAV.Json/CloudFabric.EAV.Json.csproj /src/CloudFabric.EAV.Json/CloudFabric.EAV.Json.csproj
COPY CloudFabric.EAV.Models/CloudFabric.EAV.Models.csproj /src/CloudFabric.EAV.Models/CloudFabric.EAV.Models.csproj
COPY CloudFabric.EAV.Service/CloudFabric.EAV.Service.csproj /src/CloudFabric.EAV.Service/CloudFabric.EAV.Service.csproj
COPY CloudFabric.EAV.Tests/CloudFabric.EAV.Tests.csproj /src/CloudFabric.EAV.Tests/CloudFabric.EAV.Tests.csproj

RUN dotnet restore /src/CloudFabric.EAV.Domain/CloudFabric.EAV.Domain.csproj
RUN dotnet restore /src/CloudFabric.EAV.Json/CloudFabric.EAV.Json.csproj
RUN dotnet restore /src/CloudFabric.EAV.Models/CloudFabric.EAV.Models.csproj
RUN dotnet restore /src/CloudFabric.EAV.Service/CloudFabric.EAV.Service.csproj
RUN dotnet restore /src/CloudFabric.EAV.Tests/CloudFabric.EAV.Tests.csproj
#---------------------------------------------------------------------
# /Nuget restore 
#---------------------------------------------------------------------

#---------------------------------------------------------------------
# Build artifacts
#---------------------------------------------------------------------
COPY /. /src

# Start Sonar Scanner
RUN if [ -n "$SONAR_TOKEN" ] ; then dotnet sonarscanner begin \
  /k:"$SONAR_PROJECT_KEY" \
  /o:"$SONAR_OGRANIZAION_KEY" \
  /d:sonar.host.url="$SONAR_HOST_URL" \
  /d:sonar.login="$SONAR_TOKEN" \
  /d:sonar.cs.opencover.reportsPaths=/artifacts/tests/*/coverage.opencover.xml ; fi

RUN service postgresql start && \
    dotnet test /src/CloudFabric.EAV.Tests/CloudFabric.EAV.Tests.csproj --logger trx --results-directory /artifacts/tests --configuration Release --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=json,cobertura,lcov,teamcity,opencover

ARG COVERAGE_REPORT_GENERATOR_LICENSE
ARG COVERAGE_REPORT_TITLE
ARG COVERAGE_REPORT_TAG
ARG COVERAGE_REPORT_GENERATOR_HISTORY_DIRECTORY

#RUN reportgenerator "-reports:/artifacts/tests/*/coverage.cobertura.xml" -targetdir:/artifacts/code-coverage "-reporttypes:HtmlInline_AzurePipelines_Light;SonarQube;TextSummary" "-title:$COVERAGE_REPORT_TITLE" "-tag:$COVERAGE_REPORT_TAG" "-license:$COVERAGE_REPORT_GENERATOR_LICENSE" "-historydir:$COVERAGE_REPORT_GENERATOR_HISTORY_DIRECTORY"

# End Sonar Scanner
RUN if [ -n "$SONAR_TOKEN" ] ; then dotnet sonarscanner end /d:sonar.login="$SONAR_TOKEN" ; fi

ARG PACKAGE_VERSION

RUN sed -i "s|<Version>.*</Version>|<Version>$PACKAGE_VERSION</Version>|g" /src/CloudFabric.EAV.Domain/CloudFabric.EAV.Domain.csproj && \
    sed -i "s|<Version>.*</Version>|<Version>$PACKAGE_VERSION</Version>|g" /src/CloudFabric.EAV.Json/CloudFabric.EAV.Json.csproj && \
    sed -i "s|<Version>.*</Version>|<Version>$PACKAGE_VERSION</Version>|g" /src/CloudFabric.EAV.Models/CloudFabric.EAV.Models.csproj && \
    sed -i "s|<Version>.*</Version>|<Version>$PACKAGE_VERSION</Version>|g" /src/CloudFabric.EAV.Service/CloudFabric.EAV.Service.csproj && \
    dotnet pack /src/CloudFabric.EAV.Domain/CloudFabric.EAV.Domain.csproj -o /artifacts/nugets -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg && \
    dotnet pack /src/CloudFabric.EAV.Json/CloudFabric.EAV.Json.csproj -o /artifacts/nugets -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg && \
    dotnet pack /src/CloudFabric.EAV.Models/CloudFabric.EAV.Models.csproj -o /artifacts/nugets -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg && \
    dotnet pack /src/CloudFabric.EAV.Service/CloudFabric.EAV.Service.csproj -o /artifacts/nugets -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg

ARG NUGET_API_KEY
RUN if [ -n "$NUGET_API_KEY" ] ; then dotnet nuget push /artifacts/nugets/CloudFabric.EAV.Domain.$PACKAGE_VERSION.nupkg --skip-duplicate -k $NUGET_API_KEY -s https://api.nuget.org/v3/index.json ; fi
RUN if [ -n "$NUGET_API_KEY" ] ; then dotnet nuget push /artifacts/nugets/CloudFabric.EAV.Json.$PACKAGE_VERSION.nupkg --skip-duplicate -k $NUGET_API_KEY -s https://api.nuget.org/v3/index.json ; fi
RUN if [ -n "$NUGET_API_KEY" ] ; then dotnet nuget push /artifacts/nugets/CloudFabric.EAV.Models.$PACKAGE_VERSION.nupkg --skip-duplicate -k $NUGET_API_KEY -s https://api.nuget.org/v3/index.json ; fi
RUN if [ -n "$NUGET_API_KEY" ] ; then dotnet nuget push /artifacts/nugets/CloudFabric.EAV.Service.$PACKAGE_VERSION.nupkg --skip-duplicate -k $NUGET_API_KEY -s https://api.nuget.org/v3/index.json ; fi

#---------------------------------------------------------------------
# /Build artifacts
#---------------------------------------------------------------------
