import os
import platform
import re
import logging
from pathlib import Path
from datetime import datetime, timedelta
from typing import List, Dict, Optional, Tuple

logger = logging.getLogger("unity-mcp-server")

class UnityLogAnalyzer:
    """Analyzes Unity Editor log files to detect Safe Mode and compiler errors."""
    
    def __init__(self):
        self.log_paths = self._get_unity_log_paths()
        self.safe_mode_pattern = re.compile(r"(Entering Safe Mode|ModeService.*\.ChangeMode\(safe_mode\)|MODES.*safe_mode)", re.IGNORECASE)
        self.compiler_error_pattern = re.compile(r"(error CS\d+:|Compilation failed:|CompilationPipeline\.CompileScripts|Assets.*\.cs\(\d+,\d+\): error)")
        self.unity_running_pattern = re.compile(r"Unity Editor version|LICENSE SYSTEM.*Unity runtime is initialized|COMMAND LINE ARGUMENTS.*Unity\.exe")
        
    def _get_unity_log_paths(self) -> List[Path]:
        """Get Unity Editor log file paths based on the operating system."""
        paths = []
        system = platform.system()
        
        if system == "Windows":
            # Windows paths
            local_appdata = os.environ.get('LOCALAPPDATA')
            if local_appdata:
                paths.append(Path(local_appdata) / "Unity" / "Editor" / "Editor.log")
                paths.append(Path(local_appdata) / "Unity" / "Editor" / "Editor-prev.log")
        
        elif system == "Darwin":  # macOS
            home = Path.home()
            paths.append(home / "Library" / "Logs" / "Unity" / "Editor.log")
            paths.append(home / "Library" / "Logs" / "Unity" / "Editor-prev.log")
        
        elif system == "Linux":
            home = Path.home()
            # Unity on Linux typically uses ~/.config/unity3d/Editor.log
            paths.append(home / ".config" / "unity3d" / "Editor.log")
            paths.append(home / ".config" / "unity3d" / "Editor-prev.log")
            # Alternative location
            paths.append(home / ".local" / "share" / "unity3d" / "Editor" / "Editor.log")
        
        # Filter out non-existent paths
        existing_paths = [p for p in paths if p.exists()]
        logger.debug(f"Found Unity log files: {existing_paths}")
        return existing_paths
    
    def _get_recent_log_content(self, log_path: Path, hours: int = 1) -> Optional[str]:
        """Read recent content from a log file (last N hours)."""
        try:
            # Get file modification time
            mod_time = datetime.fromtimestamp(log_path.stat().st_mtime)
            current_time = datetime.now()
            
            # Skip if file is too old
            if current_time - mod_time > timedelta(hours=24):
                logger.debug(f"Log file {log_path} is too old (modified {mod_time})")
                return None
            
            # Read the file
            with open(log_path, 'r', encoding='utf-8', errors='ignore') as f:
                content = f.read()
            
            # If the file was modified recently, return recent content
            if current_time - mod_time <= timedelta(hours=hours):
                return content
            
            # Otherwise, try to extract recent entries based on timestamps in the log
            # Unity logs typically have timestamps like: "2024-01-15 10:30:45.123"
            timestamp_pattern = re.compile(r"(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})")
            lines = content.split('\n')
            recent_lines = []
            cutoff_time = current_time - timedelta(hours=hours)
            
            for line in reversed(lines):
                match = timestamp_pattern.search(line)
                if match:
                    try:
                        line_time = datetime.strptime(match.group(1), "%Y-%m-%d %H:%M:%S")
                        if line_time < cutoff_time:
                            break
                    except:
                        pass
                recent_lines.append(line)
            
            return '\n'.join(reversed(recent_lines)) if recent_lines else content
            
        except Exception as e:
            logger.error(f"Error reading log file {log_path}: {e}")
            return None
    
    def is_unity_running(self) -> bool:
        """Check if Unity Editor is currently running based on log files."""
        for log_path in self.log_paths:
            try:
                # Check if log was modified recently (within last 30 minutes)
                mod_time = datetime.fromtimestamp(log_path.stat().st_mtime)
                if datetime.now() - mod_time <= timedelta(minutes=30):
                    # Read the log to confirm Unity started
                    content = self._get_recent_log_content(log_path, hours=1)
                    if content and self.unity_running_pattern.search(content):
                        return True
            except:
                pass
        return False
    
    def detect_safe_mode(self) -> Tuple[bool, Optional[str]]:
        """Detect if Unity is running in Safe Mode."""
        for log_path in self.log_paths:
            content = self._get_recent_log_content(log_path, hours=2)
            if content and self.safe_mode_pattern.search(content):
                logger.warning(f"Unity Safe Mode detected in {log_path}")
                return True, str(log_path)
        return False, None
    
    def get_compiler_errors(self, max_errors: int = 10) -> List[Dict[str, str]]:
        """Extract compiler errors from Unity logs."""
        errors = []
        
        for log_path in self.log_paths:
            content = self._get_recent_log_content(log_path, hours=2)
            if not content:
                continue
            
            lines = content.split('\n')
            i = 0
            while i < len(lines) and len(errors) < max_errors:
                line = lines[i]
                if self.compiler_error_pattern.search(line):
                    error_info = {
                        'line': line.strip(),
                        'context': [],
                        'log_file': str(log_path)
                    }
                    
                    # Capture context (2 lines before and after)
                    start = max(0, i - 2)
                    end = min(len(lines), i + 3)
                    error_info['context'] = [lines[j].strip() for j in range(start, end)]
                    
                    # Try to extract file path and error code
                    cs_error_match = re.search(r'(Assets.*\.cs)\((\d+),(\d+)\): error (CS\d+):', line)
                    if cs_error_match:
                        error_info['file'] = cs_error_match.group(1)
                        error_info['line_number'] = cs_error_match.group(2)
                        error_info['column'] = cs_error_match.group(3)
                        error_info['error_code'] = cs_error_match.group(4)
                    
                    errors.append(error_info)
                i += 1
        
        return errors
    
    def get_diagnostic_info(self) -> Dict[str, any]:
        """Get comprehensive diagnostic information about Unity's state."""
        info = {
            'unity_running': self.is_unity_running(),
            'log_files_found': [str(p) for p in self.log_paths],
            'safe_mode': False,
            'safe_mode_log': None,
            'compiler_errors': [],
            'analysis_time': datetime.now().isoformat()
        }
        
        # Check for safe mode
        safe_mode, safe_mode_log = self.detect_safe_mode()
        info['safe_mode'] = safe_mode
        info['safe_mode_log'] = safe_mode_log
        
        # Get compiler errors if in safe mode
        if safe_mode:
            info['compiler_errors'] = self.get_compiler_errors()
        
        return info