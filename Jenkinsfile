pipeline {
    agent any

    environment {
        SQL_CONTAINER = 'configpoc-sql'
    }

    stages {
        stage('Build') {
            steps {
                sh 'dotnet build config-poc'
            }
        }

        stage('DB_Up') {
            steps {
                withCredentials([string(credentialsId: 'MSSQL_SA_Password', variable: 'MSSQL_SA_PASSWORD')]) {
                    sh 'docker compose up -d sql-server'
                    sh '''
                        for i in $(seq 1 30); do
                            docker exec $SQL_CONTAINER /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -Q "SELECT 1" && break
                            sleep 5
                        done
                    '''
                    sh 'docker exec $SQL_CONTAINER /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -Q "IF DB_ID(\'ConfigPocDb\') IS NULL CREATE DATABASE ConfigPocDb"'
                }
            }
        }

        stage('Opcion1_ReplaceTokens') {
            steps {
                withCredentials([string(credentialsId: 'DB_ConnectionString_Dev', variable: 'CONN_STR')]) {
                    // ReplaceTokens sustituye el placeholder #{...}# directamente en el archivo,
                    // que ASP.NET Core carga vía ASPNETCORE_ENVIRONMENT=opcion1 -> appsettings.opcion1.json
                    sh '''
                        npx --yes @qetza/replacetokens --sources "config-poc/appsettings.opcion1.json" \
                          --variables "{\\"ConnectionStrings\\":{\\"DefaultConnection\\":\\"$CONN_STR\\"}}"
                    '''
                    sh '''
                        export ASPNETCORE_ENVIRONMENT=opcion1
                        dotnet run --project config-poc --no-launch-profile --urls http://localhost:5001 &
                        APP_PID=$!
                        sleep 10
                        curl -sf http://localhost:5001/config
                        curl -sf http://localhost:5001/db-check
                        kill $APP_PID
                    '''
                    sh 'git checkout -- config-poc/appsettings.opcion1.json'
                }
            }
        }

        stage('Opcion2_NativeSubstitution') {
            steps {
                withCredentials([string(credentialsId: 'DB_ConnectionString_Dev', variable: 'CONN_STR')]) {
                    // appsettings.opcion2.json queda con clave vacía; la variable de entorno
                    // (doble guion bajo) la rellena en runtime sin tocar el archivo.
                    sh '''
                        export ASPNETCORE_ENVIRONMENT=opcion2
                        export ConnectionStrings__DefaultConnection="$CONN_STR"
                        dotnet run --project config-poc --no-launch-profile --urls http://localhost:5002 &
                        APP_PID=$!
                        sleep 10
                        curl -sf http://localhost:5002/config
                        curl -sf http://localhost:5002/db-check
                        kill $APP_PID
                    '''
                }
            }
        }
    }

    post {
        always {
            sh 'docker compose down -v || true'
        }
    }
}
