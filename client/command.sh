#!/bin/bash

YELLOW='\033[0;33m'
NC='\033[0m'


echo -en "${YELLOW}"
printf "\nAvailable commands:\n\n"
printf "%-26s %s\n" "Create player:" "/player"
printf "%-26s %s\n" "Set player as ready:" "/ready"
printf "%-26s %s\n" "Start game:" "/start"
printf "%-26s %s\n" "Check room for dangers:" "/check/room"
printf "%-26s %s\n" "Check status of windows:" "/check/windows"
printf "%-26s %s\n" "Check status of doors:" "/check/doors"
printf "%-26s %s\n" "Move to kitchen:" "/move/kitchen"
printf "%-26s %s\n" "Move to hallway:" "/move/hallway"
printf "%-26s %s\n" "Move to living room:" "/move/living room"
printf "%-26s %s\n" "Lock door:" "/lock/door"
printf "%-26s %s\n" "Lock window A:" "/lock/window/A"
printf "%-26s %s\n" "Lock window B:" "/lock/window/B"
printf "%-26s %s\n" "Clear room of dangers:" "/secure/room"
printf "%-26s %s\n" "Write on whiteboard:" "/whiteboard/write"
printf "%-26s %s\n" "Read on whiteboard:" "/whiteboard/read"
printf "%-26s %s\n" "See elapsed time:" "/time"
printf "%-26s %s\n" "Restart the game" "/restart"
printf "%-26s %s\n\n\n" "See available commands:" "/help"
echo -en "${NC}"

playername=""

while true; do
	echo -en "${YELLOW}"
	read -p "Enter command (type '/exit' to quit): " command
	command=$(echo "$command" | tr '[:upper:]' '[:lower:]')
	echo -en "${NC}"

	case "$command" in
		"/help")
			echo -en "${YELLOW}"
			printf "\n%-26s %s\n" "Create player:" "/player"
			printf "%-26s %s\n" "Set player as ready:" "/ready"
			printf "%-26s %s\n" "Start game:" "/start"
			printf "%-26s %s\n" "Check room for dangers:" "/check/room"
			printf "%-26s %s\n" "Check status of windows:" "/check/windows"
			printf "%-26s %s\n" "Check status of doors:" "/check/doors"
			printf "%-26s %s\n" "Move to kitchen:" "/move/kitchen"
			printf "%-26s %s\n" "Move to hallway:" "/move/hallway"
			printf "%-26s %s\n" "Move to living room:" "/move/living room"
			printf "%-26s %s\n" "Lock door:" "/lock/door"
			printf "%-26s %s\n" "Lock window A:" "/lock/window/A"
			printf "%-26s %s\n" "Lock window B:" "/lock/window/B"
			printf "%-26s %s\n" "Clear room of dangers:" "/secure/room"
			printf "%-26s %s\n" "Write on whiteboard:" "/whiteboard/write"
			printf "%-26s %s\n" "Read on whiteboard:" "/whiteboard/read"
            printf "%-26s %s\n" "See elapsed time:" "/time"
			printf "%-26s %s\n" "Restart the game" "/restart"
			printf "%-26s %s\n\n\n" "See available commands:" "/help"
			echo -en "${NC}"
			;;
			"/")
			echo -en "${YELLOW}"
			printf "\n%-26s %s\n" "Create player:" "/player"
			printf "%-26s %s\n" "Set player as ready:" "/ready"
			printf "%-26s %s\n" "Start game:" "/start"
			printf "%-26s %s\n" "Check room for dangers:" "/check/room"
			printf "%-26s %s\n" "Check status of windows:" "/check/windows"
			printf "%-26s %s\n" "Check status of doors:" "/check/doors"
			printf "%-26s %s\n" "Move to kitchen:" "/move/kitchen"
			printf "%-26s %s\n" "Move to hallway:" "/move/hallway"
			printf "%-26s %s\n" "Move to living room:" "/move/living room"
			printf "%-26s %s\n" "Lock door:" "/lock/door"
			printf "%-26s %s\n" "Lock window A:" "/lock/window/A"
			printf "%-26s %s\n" "Lock window B:" "/lock/window/B"
			printf "%-26s %s\n" "Clear room of dangers:" "/secure/room"
			printf "%-26s %s\n" "Write on whiteboard:" "/whiteboard/write"
			printf "%-26s %s\n" "Read on whiteboard:" "/whiteboard/read"
            printf "%-26s %s\n" "See elapsed time:" "/time"
			printf "%-26s %s\n" "Restart the game" "/restart"
			printf "%-26s %s\n\n\n" "See available commands:" "/help"
			echo -en "${NC}"
			;;
		"/player")
			echo -en "${YELLOW}"
			IFS= read -r -p "Enter player name: " playername 
			echo -en "${NC}"
			curl -s -d "$playername" localhost:3000/player
			;;
		"/ready")
			curl -s -X PATCH localhost:3000/"$playername"/ready
			;;
		"/start")
			curl -s -X PATCH localhost:3000/"$playername"/start
			;;
		"/check/room")
			curl -s localhost:3000/"$playername"/room
			;;
		"/check/windows")
			curl -s localhost:3000/"$playername"/windows
			;;
		"/check/doors")
			curl -s localhost:3000/"$playername"/doors
			;;
		"/move/kitchen")
			curl -s -X PATCH -d "kitchen" localhost:3000/"$playername"/move
			;;
		"/move/hallway")
			curl -s -X PATCH -d "hallway" localhost:3000/"$playername"/move
			;;
		"/move/living room")
			curl -s -X PATCH -d "living room" localhost:3000/"$playername"/move
			;;
		"/lock/door")
			curl -s -X PATCH -d "A" localhost:3000/"$playername"/doors
			;;
		"/lock/window/a")
			curl -s -X PATCH -d "A" localhost:3000/"$playername"/windows
			;;
		"/lock/window/b")
			curl -s -X PATCH -d "B" localhost:3000/"$playername"/windows
			;;
		"/secure/room")
			curl -s -X PATCH localhost:3000/"$playername"/room
			;;
		"/whiteboard/write")
			echo -en "${YELLOW}"
			IFS= read -r -p "Enter your message: " message 
			echo -en "${NC}"
			curl -s -d "$message" localhost:3000/$playername/whiteboard
			;;
		"/whiteboard/read")
			curl -s localhost:3000/$playername/whiteboard
			;;
		"/time")
			curl -s -X PATCH localhost:3000/$playername/time
			;;
		"/restart")
			curl -s -X PATCH localhost:3000/$playername/restart
			;;
		"/exit")
			echo "Exiting game"
			break
			;;
		*)
			echo "Unknown command: $command "
			;;
	esac
done
