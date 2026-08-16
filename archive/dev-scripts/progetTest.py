#!/usr/bin/env python3
"""
ProGet test script that uploads a disposable package and measures indexing latency.
"""
import json
import os
import random
import subprocess
import sys
import tempfile
import time
from datetime import datetime
from pathlib import Path

PROGET_URL = os.getenv("PROGET_URL", "http://localhost:8624")
PROGET_FEED = os.getenv("PROGET_FEED", "fastfsm-nuget")
DEFAULT_API_KEY = "7aa315e9d1e9829caa7bfaba3f497f6c9a0b367a"
API_KEY = os.getenv("PROGET_API_KEY", DEFAULT_API_KEY)
PACKAGE_NAME = os.getenv("PROGET_PACKAGE_NAME", "TestPackage.ProGet")
MAX_WAIT_SECONDS = int(os.getenv("PROGET_WAIT_SECONDS", "60"))

SPINNER_FRAMES = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"]

def require_api_key() -> str:
    if not API_KEY:
        print("❌ Environment variable PROGET_API_KEY is required.")
        sys.exit(1)
    return API_KEY

def random_version() -> str:
    return f"1.0.{random.randint(1000, 9999)}"

def create_package(package_name: str, version: str, root: Path) -> Path:
    import zipfile

    pkg_dir = root / "pkg"
    lib_dir = pkg_dir / "lib" / "netstandard2.0"
    lib_dir.mkdir(parents=True)

    dll_path = lib_dir / f"{package_name}.dll"
    dll_path.write_text("Dummy content for ProGet test\n" * 30000)

    nuspec_template = (
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
        "<package xmlns=\"http://schemas.microsoft.com/packaging/2012/06/nuspec.xsd\">\n"
        "  <metadata>\n"
        "    <id>{id}</id>\n"
        "    <version>{version}</version>\n"
        "    <title>{id} Test Package</title>\n"
        "    <authors>ProGet Test Script</authors>\n"
        "    <owners>TestOwner</owners>\n"
        "    <requireLicenseAcceptance>false</requireLicenseAcceptance>\n"
        "    <description>Temporary package pushed by progetTest.py</description>\n"
        "    <releaseNotes>Created at {timestamp}</releaseNotes>\n"
        "    <tags>test proget indexing</tags>\n"
        "  </metadata>\n"
        "  <files>\n"
        "    <file src=\"lib/netstandard2.0/{id}.dll\" target=\"lib/netstandard2.0/\" />\n"
        "  </files>\n"
        "</package>\n"
    )

    nuspec = pkg_dir / f"{package_name}.nuspec"
    nuspec.write_text(
        nuspec_template.format(
            id=package_name,
            version=version,
            timestamp=datetime.now().isoformat(),
        )
    )

    nupkg = root / f"{package_name}.{version}.nupkg"
    with zipfile.ZipFile(nupkg, "w", zipfile.ZIP_DEFLATED) as zf:
        for path in pkg_dir.rglob("*"):
            if path.is_file():
                zf.write(path, path.relative_to(pkg_dir))
    return nupkg

def curl_json(url: str) -> dict | None:
    try:
        result = subprocess.run(["curl", "-s", url], capture_output=True, text=True, check=False)
        if result.returncode != 0 or not result.stdout.strip():
            return None
        return json.loads(result.stdout)
    except Exception:
        return None

def feed_search(package_name: str) -> dict | None:
    search_url = (
        f"{PROGET_URL}/nuget/{PROGET_FEED}/v3/search"
        f"?q={package_name}&prerelease=true&take=200&semVerLevel=2.0.0"
    )
    return curl_json(search_url)

def registration_index(package_name: str) -> dict | None:
    reg_url = (
        f"{PROGET_URL}/nuget/{PROGET_FEED}/v3/registration5-semver2/"
        f"{package_name.lower()}/index.json"
    )
    return curl_json(reg_url)

def package_download_url(package_name: str, version: str) -> str:
    lower = package_name.lower()
    return (
        f"{PROGET_URL}/nuget/{PROGET_FEED}/v3/flatcontainer/"
        f"{lower}/{version}/{lower}.{version}.nupkg"
    )

def flatcontainer_has_package(package_name: str, version: str) -> bool:
    url = package_download_url(package_name, version)
    result = subprocess.run(
        ["curl", "-s", "-o", "/dev/null", "-w", "%{http_code}", "-I", url],
        capture_output=True,
        text=True,
    )
    return result.stdout.strip() == "200"

def server_available() -> bool:
    url = f"{PROGET_URL}/nuget/{PROGET_FEED}/v3/index.json"
    response = subprocess.run(
        ["curl", "-s", "-o", "/dev/null", "-w", "%{http_code}", url],
        capture_output=True,
        text=True,
    )
    return response.stdout.strip() == "200"

def upload_package(nupkg: Path) -> tuple[bool, float, str]:
    api_key = require_api_key()
    url = f"{PROGET_URL}/nuget/{PROGET_FEED}/"
    print(f"📦 Uploading package: {nupkg.name}")
    print(f"   Size: {nupkg.stat().st_size:,} bytes")

    cmd = [
        "curl",
        "-X",
        "PUT",
        url,
        "-H",
        f"X-NuGet-ApiKey: {api_key}",
        "-F",
        f"file=@{nupkg}",
        "-s",
        "-o",
        "/dev/null",
        "-w",
        "%{http_code}",
    ]
    start = time.time()
    result = subprocess.run(cmd, capture_output=True, text=True)
    elapsed = time.time() - start
    status = result.stdout.strip()

    if status in {"200", "201", "202", "409"}:
        message = "already existed" if status == "409" else "uploaded"
        print(f"✅ Package {message} (HTTP {status}) - took {elapsed:.2f}s")
        return True, elapsed, status
    print(f"❌ Upload failed (HTTP {status}) - took {elapsed:.2f}s")
    if result.stderr:
        print(result.stderr.strip())
    return False, elapsed, status

def is_indexed(package_name: str, version: str) -> bool:
    data = feed_search(package_name)
    if not data:
        return False
    for item in data.get("data", []):
        if item.get("id", "").lower() == package_name.lower():
            versions = [entry.get("version", "") for entry in item.get("versions", [])]
            if version in versions or item.get("version") == version:
                return True
    return False

def is_available(package_name: str, version: str) -> bool:
    data = registration_index(package_name)
    if data:
        for page in data.get("items", []):
            for entry in page.get("items", []):
                catalog = entry.get("catalogEntry", {})
                if catalog.get("version") == version:
                    return True
    return flatcontainer_has_package(package_name, version)

def wait_for_indexing(package_name: str, version: str, max_wait: int) -> tuple[bool, float, float]:
    print("\n⏳ Waiting for package indexing...")
    print(f"   Package: {package_name} v{version}")
    print(f"   Feed:    {PROGET_FEED}")
    print(f"   Timeout: {max_wait}s\n")

    start = time.time()
    spinner_idx = 0
    indexed = False
    available = False
    indexing_time = float("nan")
    availability_time = float("nan")

    while time.time() - start < max_wait:
        elapsed = time.time() - start
        if not indexed and is_indexed(package_name, version):
            indexed = True
            indexing_time = elapsed
            print(f"\n✅ Package indexed after {indexing_time:.2f}s")
        if indexed and not available and is_available(package_name, version):
            available = True
            availability_time = elapsed
            print(f"✅ Package metadata available after {availability_time:.2f}s")
            break

        status = []
        status.append("Indexed ✓" if indexed else "Indexing...")
        if indexed:
            status.append("Available ✓" if available else "Waiting for availability...")
        print(
            f"\r{SPINNER_FRAMES[spinner_idx]} [{elapsed:5.1f}s] {' | '.join(status)}   ",
            end="",
            flush=True,
        )
        spinner_idx = (spinner_idx + 1) % len(SPINNER_FRAMES)
        time.sleep(1)

    print()
    if not indexed:
        indexing_time = time.time() - start
    if not available:
        availability_time = time.time() - start
    return indexed and available, indexing_time, availability_time

def download_check(package_name: str, version: str) -> bool:
    url = package_download_url(package_name, version)
    target = Path(tempfile.gettempdir()) / f"{package_name}.{version}.nupkg"
    result = subprocess.run(
        ["curl", "-s", "-o", str(target), "-w", "%{http_code}", url],
        capture_output=True,
        text=True,
    )
    if result.stdout.strip() == "200":
        target.unlink(missing_ok=True)
        return True
    return False

def main() -> None:
    print("=" * 60)
    print("🚀 ProGet Package Indexing Test")
    print("=" * 60)

    if not server_available():
        print(
            f"❌ ProGet feed not reachable at {PROGET_URL}/nuget/{PROGET_FEED}/v3/index.json"
        )
        sys.exit(1)
    print(f"✅ ProGet feed reachable at {PROGET_URL}/nuget/{PROGET_FEED}/v3/index.json")

    version = random_version()
    with tempfile.TemporaryDirectory() as tmp:
        tmp_path = Path(tmp)
        print("\n📝 Creating test package...")
        print(f"   Name:    {PACKAGE_NAME}")
        print(f"   Version: {version}")
        try:
            nupkg = create_package(PACKAGE_NAME, version, tmp_path)
            print(f"✅ Package created: {nupkg.name}")
        except Exception as exc:
            print(f"❌ Failed to create package: {exc}")
            sys.exit(1)

        print("\n" + "=" * 60)
        uploaded, upload_time, status = upload_package(nupkg)
        if not uploaded:
            sys.exit(1)

        success, indexing_time, availability_time = wait_for_indexing(
            PACKAGE_NAME,
            version,
            MAX_WAIT_SECONDS,
        )

        downloadable = download_check(PACKAGE_NAME, version) if success else False

        print("\n" + "=" * 60)
        print("📊 SUMMARY")
        print("=" * 60)
        print(f"Package:           {PACKAGE_NAME} v{version}")
        print(f"Feed:              {PROGET_FEED}")
        print(f"Upload HTTP code:  {status}")
        print(f"Upload time:       {upload_time:.2f}s")
        print(f"Indexing time:     {indexing_time:.2f}s")
        print(f"Availability time: {availability_time:.2f}s")
        result_text = "passed" if downloadable else "failed"
        print(f"Download check:    {result_text}")
        total_time = upload_time + availability_time
        print(f"Total time:        {total_time:.2f}s")

        if success and downloadable:
            print("\n✅ SUCCESS: Package is indexed and downloadable from ProGet!")
            feed_source = f"{PROGET_URL}/nuget/{PROGET_FEED}/v3/index.json"
            print("\n📥 Install via:")
            print(
                f"   dotnet add package {PACKAGE_NAME} --version {version} --source {feed_source}"
            )
        else:
            print("\n⚠️  WARNING: Package not fully available within the timeout window.")

if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print("\n⚠️  Test interrupted by user")
        sys.exit(130)
    except Exception as exc:
        print(f"\n❌ Unexpected error: {exc}")
        import traceback
        traceback.print_exc()
        sys.exit(1)
