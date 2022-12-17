#!/bin/bash

if [ "$1" != "" ]
then
    for a in $(git submodule status | awk '{print $2}')
    do
        git rm -rf $a
        rm -rf .git/modules/$a
    done

    for a in $(find . -type f -path "*/Release/*.dll") $(find . -type f -path "*.jar")
    do
        cp -r $a .
    done

    for a in $(ls -a | grep -v "^\.$" | grep -v "^\.\.$" | grep -v "^.*\.dll$" | grep -v "^.*\.jar$" | grep -v "^LICENSE$" | grep -v "^README.md$")
    do
        rm -r $a
    done
else
    git branch -D release
    git branch release
    git checkout --recurse-submodules release
    git filter-branch -f --tree-filter "bash $(realpath $0) true" --prune-empty
fi
