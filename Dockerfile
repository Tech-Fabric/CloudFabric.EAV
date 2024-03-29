FROM mcr.microsoft.com/dotnet/sdk:7.0.202-bullseye-slim AS build

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
# Test elasticsearch setup
#---------------------------------------------------------------------
RUN wget https://artifacts.elastic.co/downloads/elasticsearch/elasticsearch-8.5.0-amd64.deb
RUN wget https://artifacts.elastic.co/downloads/elasticsearch/elasticsearch-8.5.0-amd64.deb.sha512
RUN shasum -a 512 -c elasticsearch-8.5.0-amd64.deb.sha512
RUN dpkg -i elasticsearch-8.5.0-amd64.deb
# Replace home dir - needed for su
RUN sed -i "s|elasticsearch\(.*\)\/nonexistent\(.*\)|elasticsearch\1/usr/share/elasticsearch\2|g" /etc/passwd
# Replace shell
RUN sed -i "s|elasticsearch\(.*\)\/bin\/false|elasticsearch\1/bin/bash|g" /etc/passwd
RUN sed -i "s|xpack.security.enabled: true|xpack.security.enabled: false|g" /etc/elasticsearch/elasticsearch.yml
RUN sed -i "s|cluster.initial_master_nodes:\(.*\)|# cluster.initial_master_nodes:\1|g" /etc/elasticsearch/elasticsearch.yml
RUN printf '%s\n' 'cluster.routing.allocation.disk.watermark.low: "1gb"' \
    'cluster.routing.allocation.disk.watermark.high: "500mb"' \
    'cluster.routing.allocation.disk.watermark.flood_stage: "500mb"' \
    'cluster.info.update.interval: "30m"' >> /etc/elasticsearch/elasticsearch.yml
#---------------------------------------------------------------------
# /Test elasticsearch setup
#---------------------------------------------------------------------

#---------------------------------------------------------------------
# Nuget restore 
# !Important: this is a nice hack to avoid package restoration on each docker build step.
# Since we only copy nuget.config and csproj files, docker will not run restore unless nuget.config or csproj files change.
#---------------------------------------------------------------------
#COPY nuget.config /src/nuget.config

COPY CloudFabric.EAV.Domain/CloudFabric.EAV.Domain.csproj /src/CloudFabric.EAV.Domain/CloudFabric.EAV.Domain.csproj
COPY CloudFabric.EAV.Json/CloudFabric.EAV.Json.csproj /src/CloudFabric.EAV.Json/CloudFabric.EAV.Json.csproj
COPY CloudFabric.EAV.Enums/CloudFabric.EAV.Enums.csproj /src/CloudFabric.EAV.Enums/CloudFabric.EAV.Enums.csproj
COPY CloudFabric.EAV.Models/CloudFabric.EAV.Models.csproj /src/CloudFabric.EAV.Models/CloudFabric.EAV.Models.csproj
COPY CloudFabric.EAV.Options/CloudFabric.EAV.Options.csproj /src/CloudFabric.EAV.Options/CloudFabric.EAV.Options.csproj
COPY CloudFabric.EAV.Service/CloudFabric.EAV.Service.csproj /src/CloudFabric.EAV.Service/CloudFabric.EAV.Service.csproj
COPY CloudFabric.EAV.Tests/CloudFabric.EAV.Tests.csproj /src/CloudFabric.EAV.Tests/CloudFabric.EAV.Tests.csproj

RUN dotnet restore /src/CloudFabric.EAV.Domain/CloudFabric.EAV.Domain.csproj
RUN dotnet restore /src/CloudFabric.EAV.Json/CloudFabric.EAV.Json.csproj
RUN dotnet restore /src/CloudFabric.EAV.Enums/CloudFabric.EAV.Enums.csproj
RUN dotnet restore /src/CloudFabric.EAV.Models/CloudFabric.EAV.Models.csproj
RUN dotnet restore /src/CloudFabric.EAV.Options/CloudFabric.EAV.Options.csproj
RUN dotnet restore /src/CloudFabric.EAV.Service/CloudFabric.EAV.Service.csproj
RUN dotnet restore /src/CloudFabric.EAV.Tests/CloudFabric.EAV.Tests.csproj
#---------------------------------------------------------------------
# /Nuget restore 
#---------------------------------------------------------------------

#---------------------------------------------------------------------
# Build artifacts
#---------------------------------------------------------------------
COPY /. /src

ARG PULLREQUEST_TARGET_BRANCH
ARG PULLREQUEST_BRANCH
ARG PULLREQUEST_ID
ARG BRANCH_NAME

# Start Sonar Scanner
# Sonar scanner has two different modes - PR and regular with different set of options
RUN if [ -n "$SONAR_TOKEN" ] && [ -n "$PULLREQUEST_TARGET_BRANCH" ] ; then echo "Running sonarscanner in pull request mode: sonar.pullrequest.base=$PULLREQUEST_TARGET_BRANCH, sonar.pullrequest.branch=$PULLREQUEST_BRANCH, sonar.pullrequest.key=$PULLREQUEST_ID" && dotnet sonarscanner begin \
  /k:"$SONAR_PROJECT_KEY" \
  /o:"$SONAR_OGRANIZAION_KEY" \
  /d:sonar.host.url="$SONAR_HOST_URL" \
  /d:sonar.login="$SONAR_TOKEN" \
  /d:sonar.pullrequest.base="$PULLREQUEST_TARGET_BRANCH" \
  /d:sonar.pullrequest.branch="$PULLREQUEST_BRANCH" \
  /d:sonar.pullrequest.key="$PULLREQUEST_ID" \
  /d:sonar.cs.opencover.reportsPaths=/artifacts/tests/*/coverage.opencover.xml ; elif [ -n "$SONAR_TOKEN" ] ; then echo "Running sonarscanner in branch mode: sonar.branch.name=$BRANCH_NAME" && dotnet sonarscanner begin \
  /k:"$SONAR_PROJECT_KEY" \
  /o:"$SONAR_OGRANIZAION_KEY" \
  /d:sonar.host.url="$SONAR_HOST_URL" \
  /d:sonar.login="$SONAR_TOKEN" \
  /d:sonar.branch.name="$BRANCH_NAME" \
  /d:sonar.cs.opencover.reportsPaths=/artifacts/tests/*/coverage.opencover.xml ; fi


RUN su elasticsearch -c '/usr/share/elasticsearch/bin/elasticsearch' & service postgresql start && sleep 20 && \
    dotnet test /src/CloudFabric.EAV.Tests/CloudFabric.EAV.Tests.csproj --logger trx --results-directory /artifacts/tests --configuration Release --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=json,cobertura,lcov,teamcity,opencover

ARG COVERAGE_REPORT_GENERATOR_LICENSE
ARG COVERAGE_REPORT_TITLE
ARG COVERAGE_REPORT_TAG
ARG COVERAGE_REPORT_GENERATOR_HISTORY_DIRECTORY

RUN reportgenerator "-reports:/artifacts/tests/*/coverage.cobertura.xml" -targetdir:/artifacts/code-coverage "-reporttypes:HtmlInline_AzurePipelines_Light;SonarQube;TextSummary" "-title:$COVERAGE_REPORT_TITLE" "-tag:$COVERAGE_REPORT_TAG" "-license:$COVERAGE_REPORT_GENERATOR_LICENSE" "-historydir:$COVERAGE_REPORT_GENERATOR_HISTORY_DIRECTORY"

# End Sonar Scanner
RUN if [ -n "$SONAR_TOKEN" ] ; then dotnet sonarscanner end /d:sonar.login="$SONAR_TOKEN" ; fi

ARG PACKAGE_VERSION

RUN sed -i "s|<Version>.*</Version>|<Version>$PACKAGE_VERSION</Version>|g" /src/CloudFabric.EAV.Domain/CloudFabric.EAV.Domain.csproj && \
    sed -i "s|<Version>.*</Version>|<Version>$PACKAGE_VERSION</Version>|g" /src/CloudFabric.EAV.Json/CloudFabric.EAV.Json.csproj && \
    sed -i "s|<Version>.*</Version>|<Version>$PACKAGE_VERSION</Version>|g" /src/CloudFabric.EAV.Enums/CloudFabric.EAV.Enums.csproj && \
    sed -i "s|<Version>.*</Version>|<Version>$PACKAGE_VERSION</Version>|g" /src/CloudFabric.EAV.Models/CloudFabric.EAV.Models.csproj && \
    sed -i "s|<Version>.*</Version>|<Version>$PACKAGE_VERSION</Version>|g" /src/CloudFabric.EAV.Options/CloudFabric.EAV.Options.csproj && \
    sed -i "s|<Version>.*</Version>|<Version>$PACKAGE_VERSION</Version>|g" /src/CloudFabric.EAV.Service/CloudFabric.EAV.Service.csproj && \
    dotnet pack /src/CloudFabric.EAV.Domain/CloudFabric.EAV.Domain.csproj -o /artifacts/nugets -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg && \
    dotnet pack /src/CloudFabric.EAV.Json/CloudFabric.EAV.Json.csproj -o /artifacts/nugets -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg && \
    dotnet pack /src/CloudFabric.EAV.Enums/CloudFabric.EAV.Enums.csproj -o /artifacts/nugets -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg && \
    dotnet pack /src/CloudFabric.EAV.Models/CloudFabric.EAV.Models.csproj -o /artifacts/nugets -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg && \
    dotnet pack /src/CloudFabric.EAV.Options/CloudFabric.EAV.Options.csproj -o /artifacts/nugets -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg && \
    dotnet pack /src/CloudFabric.EAV.Service/CloudFabric.EAV.Service.csproj -o /artifacts/nugets -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg

ARG NUGET_API_KEY
RUN if [ -n "$NUGET_API_KEY" ] ; then dotnet nuget push /artifacts/nugets/CloudFabric.EAV.Domain.$PACKAGE_VERSION.nupkg --skip-duplicate -k $NUGET_API_KEY -s https://api.nuget.org/v3/index.json ; fi
RUN if [ -n "$NUGET_API_KEY" ] ; then dotnet nuget push /artifacts/nugets/CloudFabric.EAV.Json.$PACKAGE_VERSION.nupkg --skip-duplicate -k $NUGET_API_KEY -s https://api.nuget.org/v3/index.json ; fi
RUN if [ -n "$NUGET_API_KEY" ] ; then dotnet nuget push /artifacts/nugets/CloudFabric.EAV.Enums.$PACKAGE_VERSION.nupkg --skip-duplicate -k $NUGET_API_KEY -s https://api.nuget.org/v3/index.json ; fi
RUN if [ -n "$NUGET_API_KEY" ] ; then dotnet nuget push /artifacts/nugets/CloudFabric.EAV.Models.$PACKAGE_VERSION.nupkg --skip-duplicate -k $NUGET_API_KEY -s https://api.nuget.org/v3/index.json ; fi
RUN if [ -n "$NUGET_API_KEY" ] ; then dotnet nuget push /artifacts/nugets/CloudFabric.EAV.Options.$PACKAGE_VERSION.nupkg --skip-duplicate -k $NUGET_API_KEY -s https://api.nuget.org/v3/index.json ; fi
RUN if [ -n "$NUGET_API_KEY" ] ; then dotnet nuget push /artifacts/nugets/CloudFabric.EAV.Service.$PACKAGE_VERSION.nupkg --skip-duplicate -k $NUGET_API_KEY -s https://api.nuget.org/v3/index.json ; fi

#---------------------------------------------------------------------
# /Build artifacts
#---------------------------------------------------------------------
