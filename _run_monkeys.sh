#!/bin/bash

function pause () {
    read -n1 -p "Press any key to continue . . ."
    echo
}
set -v

cd "`dirname "$0"`"

dotnet run -p Monkeys.csproj

pause

