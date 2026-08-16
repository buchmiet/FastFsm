#!/usr/bin/env python3
"""
BaGet Test Script - Tests package upload and indexing time
"""
import time
import subprocess
import json
import sys
import tempfile
import shutil
from pathlib import Path
from datetime import datetime
import random
import string
import xml.etree.ElementTree as ET

# Configuration
BAGET_URL = "http://localhost:5555"
API_KEY = "NUGET-SERVER-API-KEY"
PACKAGE_NAME = "TestPackage.BaGet"

def generate_random_version():
    """Generate a random version number"""
    return f"1.0.{random.randint(1000, 9999)}"

def create_test_package(package_name: str, version: str, temp_dir: Path) -> Path:
    """Create a simple test NuGet package"""
    import zipfile
    import os

    # Create package directory structure
    pkg_dir = temp_dir / "package"
    lib_dir = pkg_dir / "lib" / "netstandard2.0"
    lib_dir.mkdir(parents=True)

    # Create a dummy DLL (larger file for testing)
    dll_file = lib_dir / f"{package_name}.dll"
    # Create a larger file (1MB) to simulate real package
    dll_content = "This is a dummy DLL for testing\n" * 30000
    dll_file.write_text(dll_content)

    # Create .nuspec file
    nuspec_content = f"""<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2012/06/nuspec.xsd">
  <metadata>
    <id>{package_name}</id>
    <version>{version}</version>
    <title>{package_name} Test Package</title>
    <authors>BaGet Test Script</authors>
    <owners>TestOwner</owners>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>Test package for BaGet indexing time measurement</description>
    <releaseNotes>Test release at {datetime.now().isoformat()}</releaseNotes>
    <copyright>Copyright {datetime.now().year}</copyright>
    <tags>test baget indexing</tags>
    <dependencies>
      <group targetFramework="netstandard2.0" />
    </dependencies>
  </metadata>
  <files>
    <file src="lib/netstandard2.0/{package_name}.dll" target="lib/netstandard2.0/" />
  </files>
</package>"""

    nuspec_file = pkg_dir / f"{package_name}.nuspec"
    nuspec_file.write_text(nuspec_content)

    # Create the nupkg file (which is just a zip)
    nupkg_path = temp_dir / f"{package_name}.{version}.nupkg"

    # Create ZIP archive using Python's zipfile module
    with zipfile.ZipFile(nupkg_path, 'w', zipfile.ZIP_DEFLATED) as zipf:
        # Walk through the package directory and add all files
        for root, dirs, files in os.walk(pkg_dir):
            for file in files:
                file_path = Path(root) / file
                # Calculate the archive name (relative to pkg_dir)
                arcname = file_path.relative_to(pkg_dir)
                zipf.write(file_path, arcname)

    return nupkg_path

def upload_package(nupkg_path: Path) -> tuple[bool, float]:
    """Upload package to BaGet and return (success, time_taken)"""

    print(f"📦 Uploading package: {nupkg_path.name}")
    print(f"   Size: {nupkg_path.stat().st_size:,} bytes")

    start_time = time.time()

    cmd = [
        "curl", "-X", "PUT",
        f"{BAGET_URL}/api/v2/package",
        "-H", f"X-NuGet-ApiKey: {API_KEY}",
        "-F", f"file=@{nupkg_path}",
        "-s", "-o", "/dev/null", "-w", "%{http_code}"
    ]

    result = subprocess.run(cmd, capture_output=True, text=True)
    upload_time = time.time() - start_time

    http_code = result.stdout.strip()

    if http_code in ["201", "202"]:
        print(f"✅ Upload successful (HTTP {http_code}) - took {upload_time:.2f}s")
        return True, upload_time
    elif http_code == "409":
        print(f"⚠️  Package already exists (HTTP 409) - took {upload_time:.2f}s")
        return True, upload_time
    else:
        print(f"❌ Upload failed (HTTP {http_code}) - took {upload_time:.2f}s")
        return False, upload_time

def search_package(package_name: str, version: str) -> bool:
    """Search for package in BaGet index"""

    search_url = f"{BAGET_URL}/v3/search?q={package_name}&prerelease=true&take=100"

    try:
        result = subprocess.run(
            ["curl", "-s", search_url],
            capture_output=True,
            text=True
        )

        data = json.loads(result.stdout)

        # Check if our package is in the results
        for item in data.get("data", []):
            if item.get("id", "").lower() == package_name.lower():
                # Check if our version is listed
                versions = [v.get("version", "") for v in item.get("versions", [])]
                if version in versions:
                    return True

        return False
    except Exception as e:
        return False

def check_package_availability(package_name: str, version: str) -> bool:
    """Check if package can be downloaded"""

    # Try to get package metadata
    metadata_url = f"{BAGET_URL}/v3/registration/{package_name.lower()}/index.json"

    try:
        result = subprocess.run(
            ["curl", "-s", metadata_url],
            capture_output=True,
            text=True
        )

        if result.returncode != 0:
            return False

        data = json.loads(result.stdout)

        # Check if version exists in metadata
        for page in data.get("items", []):
            for item in page.get("items", []):
                catalog_entry = item.get("catalogEntry", {})
                if catalog_entry.get("version", "") == version:
                    return True

        return False
    except Exception:
        return False

def wait_for_indexing(package_name: str, version: str, max_wait: int = 60) -> tuple[bool, float, float]:
    """
    Wait for package to be indexed and available
    Returns: (success, indexing_time, availability_time)
    """

    print(f"\n⏳ Waiting for package indexing...")
    print(f"   Package: {package_name} v{version}")
    print(f"   Max wait time: {max_wait}s\n")

    start_time = time.time()
    indexed = False
    available = False
    indexing_time = None
    availability_time = None

    spinner = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"]
    spinner_idx = 0

    while time.time() - start_time < max_wait:
        elapsed = time.time() - start_time

        # Check if indexed (appears in search)
        if not indexed:
            if search_package(package_name, version):
                indexed = True
                indexing_time = elapsed
                print(f"\n✅ Package indexed after {indexing_time:.2f}s")

        # Check if available for download
        if indexed and not available:
            if check_package_availability(package_name, version):
                available = True
                availability_time = elapsed
                print(f"✅ Package available for download after {availability_time:.2f}s")
                break

        # Show progress
        status = []
        if indexed:
            status.append("Indexed ✓")
        else:
            status.append("Indexing...")

        if indexed and not available:
            status.append("Waiting for availability...")
        elif available:
            status.append("Available ✓")

        print(f"\r{spinner[spinner_idx]} [{elapsed:5.1f}s] {' | '.join(status)}     ", end="", flush=True)
        spinner_idx = (spinner_idx + 1) % len(spinner)

        time.sleep(1)

    print()  # New line after progress

    if not indexed:
        indexing_time = time.time() - start_time
    if not available:
        availability_time = time.time() - start_time

    return indexed and available, indexing_time, availability_time

def main():
    print("=" * 60)
    print("🚀 BaGet Package Indexing Test")
    print("=" * 60)

    # Check BaGet availability
    print("\n🔍 Checking BaGet server...")
    try:
        result = subprocess.run(
            ["curl", "-s", "-o", "/dev/null", "-w", "%{http_code}", f"{BAGET_URL}/v3/index.json"],
            capture_output=True,
            text=True
        )
        if result.stdout.strip() != "200":
            print(f"❌ BaGet server not responding at {BAGET_URL}")
            sys.exit(1)
        print(f"✅ BaGet server is running at {BAGET_URL}")
    except Exception as e:
        print(f"❌ Failed to connect to BaGet: {e}")
        sys.exit(1)

    # Generate random version to avoid conflicts
    version = generate_random_version()

    # Create temporary directory
    with tempfile.TemporaryDirectory() as temp_dir:
        temp_path = Path(temp_dir)

        print(f"\n📝 Creating test package...")
        print(f"   Name: {PACKAGE_NAME}")
        print(f"   Version: {version}")

        # Create test package
        try:
            nupkg_path = create_test_package(PACKAGE_NAME, version, temp_path)
            print(f"✅ Test package created: {nupkg_path.name}")
        except Exception as e:
            print(f"❌ Failed to create package: {e}")
            sys.exit(1)

        # Upload package
        print("\n" + "=" * 60)
        upload_success, upload_time = upload_package(nupkg_path)

        if not upload_success:
            print("❌ Upload failed, exiting...")
            sys.exit(1)

        # Wait for indexing
        print("=" * 60)
        success, indexing_time, availability_time = wait_for_indexing(
            PACKAGE_NAME, version, max_wait=60
        )

        # Print summary
        print("\n" + "=" * 60)
        print("📊 SUMMARY")
        print("=" * 60)
        print(f"Package:           {PACKAGE_NAME} v{version}")
        print(f"Upload time:       {upload_time:.2f}s")
        print(f"Indexing time:     {indexing_time:.2f}s")
        print(f"Availability time: {availability_time:.2f}s")
        print(f"Total time:        {upload_time + availability_time:.2f}s")

        if success:
            print(f"\n✅ SUCCESS: Package is fully indexed and available!")

            # Show how to download
            print(f"\n📥 You can now install the package with:")
            print(f"   dotnet add package {PACKAGE_NAME} --version {version} --source {BAGET_URL}/v3/index.json")
        else:
            print(f"\n⚠️  WARNING: Package indexing incomplete after {availability_time:.2f}s")
            if indexing_time < availability_time:
                print("   - Package was indexed in search")
            else:
                print("   - Package was NOT indexed in search")

            if availability_time < 60:
                print("   - Package metadata is available")
            else:
                print("   - Package metadata is NOT available")

if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print("\n\n⚠️  Test interrupted by user")
        sys.exit(130)
    except Exception as e:
        print(f"\n❌ Unexpected error: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)