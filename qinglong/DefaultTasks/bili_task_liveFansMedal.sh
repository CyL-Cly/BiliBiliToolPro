#!/usr/bin/env bash
# cron:5 0 * * *
# new Env("bili直播粉丝牌")

. "$(dirname "${BASH_SOURCE[0]}")/bili_task_base.sh"

target_task_code="LiveFansMedal"
run_task "${target_task_code}"