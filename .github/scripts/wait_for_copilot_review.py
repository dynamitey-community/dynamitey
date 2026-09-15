#!/usr/bin/env python3
"""Wait until Copilot's latest review of HEAD recommends merge.

Copilot posts a Comment review, not Approve. The merge signal is the
"Approval recommended" heading (or an actual APPROVED state). Exit 0 only
when that review is of HEAD and no Copilot threads remain unresolved.
"""

from __future__ import annotations

import json
import os
import subprocess
import sys
import time


def copilot(login: str | None) -> bool:
    if not login:
        return False
    return login.lower().replace("[bot]", "") in {
        "copilot-pull-request-reviewer",
        "copilot",
    }


def load(owner: str, name: str, pr: int) -> dict:
    query = """
    query($owner: String!, $name: String!, $n: Int!) {
      repository(owner: $owner, name: $name) {
        pullRequest(number: $n) {
          reviews(last: 50) {
            nodes {
              author { login }
              state
              body
              submittedAt
              commit { oid }
            }
          }
          reviewThreads(last: 80) {
            nodes {
              isResolved
              comments(first: 1) {
                nodes { author { login } }
              }
            }
          }
        }
      }
    }
    """
    raw = subprocess.check_output(
        [
            "gh",
            "api",
            "graphql",
            "-f",
            f"query={query}",
            "-F",
            f"owner={owner}",
            "-F",
            f"name={name}",
            "-F",
            f"n={pr}",
        ],
        text=True,
    )
    return json.loads(raw)["data"]["repository"]["pullRequest"]


def main() -> int:
    owner, name = os.environ["GITHUB_REPOSITORY"].split("/", 1)
    pr = int(os.environ["PR_NUMBER"])
    want_sha = os.environ["HEAD_SHA"].lower()
    timeout = int(os.environ.get("TIMEOUT_SECONDS", "900"))
    interval = int(os.environ.get("POLL_SECONDS", "20"))

    deadline = time.time() + timeout
    last = f"Copilot has not reviewed {want_sha[:8]} yet"
    while time.time() < deadline:
        data = load(owner, name, pr)
        reviews = [
            r
            for r in data["reviews"]["nodes"]
            if copilot((r.get("author") or {}).get("login"))
        ]
        unresolved = 0
        for thread in data["reviewThreads"]["nodes"]:
            if thread["isResolved"] or not thread["comments"]["nodes"]:
                continue
            if copilot(
                (thread["comments"]["nodes"][0].get("author") or {}).get("login")
            ):
                unresolved += 1

        on_head = [
            r
            for r in reviews
            if ((r.get("commit") or {}).get("oid") or "").lower() == want_sha
        ]
        latest = on_head[-1] if on_head else None
        if latest is None:
            last = (
                f"Copilot has not reviewed {want_sha[:8]} yet "
                f"({len(reviews)} Copilot review(s) on other SHAs)"
            )
            print(last, flush=True)
            time.sleep(interval)
            continue

        body = latest.get("body") or ""
        state = latest.get("state") or ""
        print(
            f"Copilot {state} on {want_sha[:8]} at {latest.get('submittedAt')}",
            flush=True,
        )
        for line in body.splitlines()[:8]:
            print(line, flush=True)

        if "unable to review" in body.lower():
            print(
                "Copilot failed to review this pull request. "
                "Re-request the Copilot review.",
                file=sys.stderr,
            )
            return 1
        if "Changes recommended" in body or state == "CHANGES_REQUESTED":
            print(
                "Copilot recommended changes. Address them, push, "
                "and wait for Copilot to review HEAD again.",
                file=sys.stderr,
            )
            return 1
        if unresolved:
            print(
                f"{unresolved} unresolved Copilot thread(s). "
                "Push a fix or leave the thread open until Copilot re-reviews.",
                file=sys.stderr,
            )
            return 1
        if state == "APPROVED" or "Approval recommended" in body:
            print("Copilot recommended merge.", flush=True)
            return 0

        last = f"Copilot reviewed HEAD as {state} without Approval recommended"
        print(last, flush=True)
        time.sleep(interval)

    print(f"Timed out after {timeout}s: {last}", file=sys.stderr)
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
