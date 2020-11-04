#!/bin/bash

function pause () {
    read -n1 -p "Press any key to continue . . ."
    echo
}
set -v

cd "`dirname "$0"`"

dotnet run -p Client.csproj < /dev/null
pause

dotnet run -p Client.csproj < tt-test.txt
pause

dotnet run -p Client.csproj -- 8102 < /dev/null
pause

