#!/bin/bash

# Default values
TIMEOUT_MIN=5
MODEL=""
QUIET=false
TASK=""

# Function to display usage
usage() {
    echo "Usage: $0 [-m model] [-q] [-t timeout_minutes] [task_description]"
    echo "  -m model    : Specify the model to use"
    echo "  -q          : Quiet mode (suppress stderr)"
    echo "  -t minutes  : Timeout in minutes (default: 5, max: 10)"
    echo "  task_description : The task prompt (optional if provided via stdin)"
    exit 1
}

# Parse options
while getopts ":m:t:q" opt; do
  case ${opt} in
    m)
      MODEL="${OPTARG}"
      ;;
    t)
      TIMEOUT_MIN="${OPTARG}"
      ;;
    q)
      QUIET=true
      ;;
    \?)
      echo "Invalid option: -${OPTARG}" >&2
      usage
      ;;
    :)
      echo "Option -${OPTARG} requires an argument." >&2
      usage
      ;;
  esac
done
shift $((OPTIND -1))

# Validate and cap timeout
if ! [[ "$TIMEOUT_MIN" =~ ^[0-9]+$ ]]; then
    echo "Error: Timeout must be an integer (minutes)." >&2
    exit 1
fi

if [ "$TIMEOUT_MIN" -gt 10 ]; then
    echo "Warning: Timeout of $TIMEOUT_MIN minutes exceeds limit. Capping at 10 minutes." >&2
    TIMEOUT_MIN=10
fi

# Convert minutes to seconds for the timeout command
TIMEOUT_SEC=$((TIMEOUT_MIN * 60))

# Get task description from argument or stdin
if [ $# -ge 1 ]; then
    TASK="$*"
else
    # Check if stdin has data
    if [ ! -t 0 ]; then
        TASK=$(cat)
    fi
fi

if [ -z "$TASK" ]; then
    echo "Error: No task description provided." >&2
    usage
fi

# Build the gemini command
CMD="gemini"
ARGS=("--yolo" "-o" "text")

if [ -n "$MODEL" ]; then
    ARGS+=("--model" "$MODEL")
fi

# Add the task as the last argument
ARGS+=("$TASK")

# Execution
if [ "$QUIET" = true ]; then
    # Run with timeout, capture stdout, suppress stderr
    OUTPUT=$(timeout "$TIMEOUT_SEC" "$CMD" "${ARGS[@]}" 2>/dev/null)
    CMD_EXIT_CODE=$?
else
    # Run with timeout, capture stdout, allow stderr to pass through
    OUTPUT=$(timeout "$TIMEOUT_SEC" "$CMD" "${ARGS[@]}")
    CMD_EXIT_CODE=$?
fi

# Check for timeout (exit code 124 is standard for GNU timeout)
if [ $CMD_EXIT_CODE -eq 124 ]; then
    if [ "$QUIET" = false ]; then
        echo "Error: Operation timed out after ${TIMEOUT_MIN} minutes." >&2
    fi
    exit 124
fi

# Print the captured output
echo "$OUTPUT"

# Exit with the command's exit code
exit $CMD_EXIT_CODE
