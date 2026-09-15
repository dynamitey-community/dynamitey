#!/usr/bin/env python3
"""Wait until Copilot's latest review of HEAD recommends merge.

Copilot posts a Comment review, not Approve. The merge signal is an
APPROVED state, or a COMMENTED review whose body says "Approval
recommended". DISMISSED reviews never pass. Exit 0 only when that
review is of HEAD and no Copilot threads remain unresolved.
"""

from __future__ import annotations

import json
import os
import subprocess
import sys
import time

COPILOT_LOGINS = {
    "copilot-pull-request-reviewer",
    "copilot",
}

QUERY = """
query($owner: String!, $name: String!, $n: Int!, $reviewsAfter: String, $threadsAfter: String) {
  repository(owner: $owner, name: $name) {
    pullRequest(number: $n) {
      reviews(first: 100, after: $reviewsAfter) {
        pageInfo { hasNextPage endCursor }
        nodes {
          author { login }
          state
          body
          submittedAt
          commit { oid }
        }
      }
      reviewThreads(first: 100, after: $threadsAfter) {
        pageInfo { hasNextPage endCursor }
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


def copilot(login: str | None) -> bool:
    if not login:
        return False
    return login.lower().replace("[bot]", "") in COPILOT_LOGINS


def gql(owner: str, name: str, pr: int, reviews_after: str | None, threads_after: str | None) -> dict:
    cmd = [
        "gh",
        "api",
        "graphql",
        "-f",
        f"query={QUERY}",
        "-F",
        f"owner={owner}",
        "-F",
        f"name={name}",
        "-F",
        f"n={pr}",
    ]
    if reviews_after:
        cmd.extend(["-F", f"reviewsAfter={reviews_after}"])
    if threads_after:
        cmd.extend(["-F", f"threadsAfter={threads_after}"])
    try:
        raw = subprocess.check_output(cmd, text=True)
    except subprocess.CalledProcessError as exc:
        raise RuntimeError("GraphQL pagination failed; refusing to pass closed") from exc
    payload = json.loads(raw)
    if payload.get("errors"):
        raise RuntimeError(f"GraphQL errors: {payload['errors']}")
    return payload["data"]["repository"]["pullRequest"]


def load_all(owner: str, name: str, pr: int) -> tuple[list[dict], list[dict]]:
    reviews: list[dict] = []
    threads: list[dict] = []

    reviews_after: str | None = None
    while True:
        data = gql(owner, name, pr, reviews_after, None)
        conn = data["reviews"]
        reviews.extend(conn["nodes"])
        if not conn["pageInfo"]["hasNextPage"]:
            break
        reviews_after = conn["pageInfo"]["endCursor"]
        if not reviews_after:
            raise RuntimeError("reviews hasNextPage without endCursor; refusing to pass")

    threads_after: str | None = None
    while True:
        data = gql(owner, name, pr, None, threads_after)
        conn = data["reviewThreads"]
        threads.extend(conn["nodes"])
        if not conn["pageInfo"]["hasNextPage"]:
            break
        threads_after = conn["pageInfo"]["endCursor"]
        if not threads_after:
            raise RuntimeError("reviewThreads hasNextPage without endCursor; refusing to pass")

    return reviews, threads


def main() -> int:
    owner, name = os.environ["GITHUB_REPOSITORY"].split("/", 1)
    pr = int(os.environ["PR_NUMBER"])
    want_sha = os.environ["HEAD_SHA"].lower()
    timeout = int(os.environ.get("TIMEOUT_SECONDS", "900"))
    interval = int(os.environ.get("POLL_SECONDS", "20"))

    deadline = time.time() + timeout
    last = f"Copilot has not reviewed {want_sha[:8]} yet"
    while time.time() < deadline:
        try:
            all_reviews, threads = load_all(owner, name, pr)
        except RuntimeError as exc:
            print(str(exc), file=sys.stderr)
            return 1

        reviews = [
            r
            for r in all_reviews
            if copilot((r.get("author") or {}).get("login"))
        ]
        unresolved = 0
        for thread in threads:
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
            and (r.get("state") or "") != "DISMISSED"
        ]
        on_head.sort(key=lambda r: r.get("submittedAt") or "")
        latest = on_head[-1] if on_head else None
        if latest is None:
            last = (
                f"Copilot has not reviewed {want_sha[:8]} yet "
                f"({len(reviews)} Copilot review(s) including dismissed/other SHAs)"
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
        if state not in {"COMMENTED", "APPROVED"}:
            last = f"Copilot reviewed HEAD as {state}; waiting for COMMENTED or APPROVED"
            print(last, flush=True)
            time.sleep(interval)
            continue
        if state != "APPROVED" and "Approval recommended" not in body:
            last = f"Copilot reviewed HEAD as {state} without Approval recommended"
            print(last, flush=True)
            time.sleep(interval)
            continue
        if unresolved:
            last = (
                f"Copilot recommended merge, but {unresolved} Copilot "
                "thread(s) are still open; waiting for resolution"
            )
            print(last, flush=True)
            time.sleep(interval)
            continue

        print("Copilot recommended merge.", flush=True)
        return 0

    print(f"Timed out after {timeout}s: {last}", file=sys.stderr)
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
