# Try block
set HAVE_AESGCM 1
# Catch Exception
set AESGCM ""
set Scrypt ""
set HAVE_AESGCM 0
# Try block
set HAVE_ARGON2 1
# Catch Exception
set argon2ll ""
set HAVE_ARGON2 0
# Try block
# Catch Exception
set HAVE_BROTLI 0
# Try block
# Catch Exception
set HAVE_ZSTD 0
# Try block
# Catch Exception
set HAVE_BLAKE3 0
# Try block
# Catch Exception
set HAVE_XXH 0
set WIN [expr {$::tcl_platform(os) == "Windows"}]
set LIN [string match "linux*" $::tcl_platform(platform)]
if {$WIN} {
    # Try block
    set HAVE_WIN32 1
    # Catch Exception
    set HAVE_WIN32 0
} else {
    set HAVE_WIN32 0
}
set LOG_COL_PIPE 48
# Class VLog
set LEVELS [dict create "quiet" 0 "error" 1 "warning" 2 "info" 3 "debug" 4 "trace" 5]
proc __init__ {self level} {
    setattr $self "level" ::get ::LEVELS $self $level 2
}
proc _fmt {self level_name msg} {
    set now [string range ::strftime ::now $datetime "%m/%d/%Y %H:%M:%S.%f" 0 [expr {- 1}]]
    set prefix "[VFA {level_name.upper():<7}] {now}"
    set pad [expr {[expr {$LOG_COL_PIPE - len $prefix}] - 1}]
    if {[expr {$pad < 1}]} {
        set pad 1
    }
    "{prefix}{' ' * pad}| {msg}"
}
proc _emit {self lvl name msg} {
    if {[expr {::level $self >= $lvl}]} {
        puts ::_fmt $self $name $msg
    }
}
proc error {self msg} {
    ::_emit $self 1 "ERROR" $msg
}
proc warning {self msg} {
    ::_emit $self 2 "WARNING" $msg
}
proc info {self msg} {
    ::_emit $self 3 "INFO" $msg
}
proc debug {self msg} {
    ::_emit $self 4 "DEBUG" $msg
}
proc trace {self msg} {
    ::_emit $self 5 "TRACE" $msg
}
set LOGGER VLog
proc human_bytes {n} {
    set units [list "B" "KiB" "MiB" "GiB" "TiB" "PiB"]
    set v float $n
    set i 0
    while {[expr {[expr {$v >= 1024}] and [expr {$i < [expr {len $units - 1}]}]}]} {
        set v [expr {$v / 1024}]
        set i [expr {$i + 1}]
    }
    "{v:.2f} {units[i]}"
}
# Class Progress
proc __init__ {self total_files total_bytes} {
    setattr $self "total_files" $total_files
    setattr $self "total_bytes" $total_bytes
    setattr $self "done_files" 0
    setattr $self "done_bytes" 0
    setattr $self "start_ts" ::time $time
}
proc add_file {self size duration_s} {
    setattr $self "done_files" [expr {getattr $self "done_files" + 1}]
    setattr $self "done_bytes" [expr {getattr $self "done_bytes" + $size}]
}
proc estimate {self} {
    set elapsed [expr {::time $time - ::start_ts $self}]
    set rate [expr {[expr {$elapsed > 0}] ? [expr {::done_bytes $self / $elapsed}] : 0}]
    set remain_bytes max 0 [expr {::total_bytes $self - ::done_bytes $self}]
    set eta [expr {[expr {$rate > 0}] ? [expr {$remain_bytes / $rate}] : float "inf"}]
    set ratio [expr {[expr {::total_bytes $self > 0}] ? [expr {::done_bytes $self / ::total_bytes $self}] : 0}]
    [list $elapsed $eta $rate $ratio]
}
set MAGIC "VFA1"
set END_MAGIC "/VFA1"
set VERSION 1
set AEAD_NONE 0
set AEAD_AESGCM 1
set KDF_NONE 0
set KDF_ARGON2ID 1
set KDF_SCRYPT 2
set M_NONE 0
set M_ZLIB 1
set M_LZMA 2
set M_BROTLI 3
set M_ZSTD 4
set METHOD_NAMES [dict create $M_NONE "none" $M_ZLIB "zlib" $M_LZMA "lzma" $M_BROTLI "brotli" $M_ZSTD "zstd"]
set NAME_TO_METHOD [dict create {*}[set _result [dict create]; foreach {k v} [set _items [list]; foreach k [dict keys $METHOD_NAMES] {lappend _items $k [dict get $METHOD_NAMES $k]}; set _items] {dict set _result $v $k}; set _result]]
set F_ENCRYPTED [expr {1 << 0}]
set F_SOLID [expr {1 << 1}]
set H_NONE 0
set H_SHA256 1
set H_BLAKE3 2
set H_XXH64 3
proc default_hash_kind {} {
    if {$HAVE_XXH} {
        $H_XXH64
    }
    if {$HAVE_BLAKE3} {
        $H_BLAKE3
    }
    $H_SHA256
}
proc make_hasher {kind} {
    if {[expr {$kind == $H_XXH64}]} {
        if {[expr {not $HAVE_XXH}]} {
            error RuntimeError "xxhash not installed"
        }
    }
    if {[expr {$kind == $H_BLAKE3}]} {
        if {[expr {not $HAVE_BLAKE3}]} {
            error RuntimeError "blake3 not installed"
        }
    }
    if {[expr {$kind == $H_SHA256}]} {
        ::sha256 $hashlib
    }
    error RuntimeError "bad hash kind"
}
proc hasher_update {h data kind} {
    ::update $h $data
}
proc hasher_digest {h kind} {
    if {[expr {$kind == $H_XXH64}]} {
        [expr {::digest $h + [string repeat "x00" 24]}]
    }
    ::digest $h
}
proc nonce_from {prefix12 index} {
    set m ::sha256 $hashlib
    ::update $m $prefix12
    ::update $m ::pack $struct "<Q" $index
    ::update $m "vfa-nonce"
    [string range ::digest $m 0 12]
}
# Class HeaderV1
set version $VERSION
set flags 0
set default_method [expr {$HAVE_ZSTD ? $M_ZSTD : $M_ZLIB}]
set default_level 5
set block_exp 22
set threads_hint 0
set ram_mib_hint 0
set kdf_id $KDF_NONE
set kdf_t 0
set kdf_m 0
set kdf_p 0
set salt [string repeat "x00" 16]
set aead_id $AEAD_NONE
set aead_nonce_prefix [string repeat "x00" 12]
set reserved [string repeat "x00" 16]
proc pack {self} {
    [join [list $MAGIC ::pack $struct "<H" ::version $self ::pack $struct "<I" ::flags $self ::pack $struct "<B" ::default_method $self ::pack $struct "<B" ::default_level $self ::pack $struct "<B" ::block_exp $self ::pack $struct "<H" ::threads_hint $self ::pack $struct "<I" ::ram_mib_hint $self ::pack $struct "<B" ::kdf_id $self ::pack $struct "<I" ::kdf_t $self ::pack $struct "<I" ::kdf_m $self ::pack $struct "<B" ::kdf_p $self ::salt $self ::pack $struct "<B" ::aead_id $self ::aead_nonce_prefix $self ::reserved $self] ""]
}
proc unpack {cls bio} {
    if {[expr {::read $bio 4 != $MAGIC}]} {
        error ValueError "Not a VFA archive"
    }
    set _tuple ::unpack $struct "<H" ::read $bio 2
    lassign $_tuple version
    set _tuple ::unpack $struct "<I" ::read $bio 4
    lassign $_tuple flags
    set _tuple ::unpack $struct "<B" ::read $bio 1
    lassign $_tuple dm
    set _tuple ::unpack $struct "<B" ::read $bio 1
    lassign $_tuple dl
    set _tuple ::unpack $struct "<B" ::read $bio 1
    lassign $_tuple be
    set _tuple ::unpack $struct "<H" ::read $bio 2
    lassign $_tuple th
    set _tuple ::unpack $struct "<I" ::read $bio 4
    lassign $_tuple rm
    set _tuple ::unpack $struct "<B" ::read $bio 1
    lassign $_tuple kid
    set _tuple ::unpack $struct "<I" ::read $bio 4
    lassign $_tuple kt
    set _tuple ::unpack $struct "<I" ::read $bio 4
    lassign $_tuple km
    set _tuple ::unpack $struct "<B" ::read $bio 1
    lassign $_tuple kp
    set salt ::read $bio 16
    set _tuple ::unpack $struct "<B" ::read $bio 1
    lassign $_tuple aid
    set np ::read $bio 12
    set res ::read $bio 16
    cls $version $flags $dm $dl $be $th $rm $kid $kt $km $kp $salt $aid $np $res
}
set ET_FILE 0
set ET_DIR 1
set ET_SYMLINK 2
set ET_HARDLINK 3
# Class FileEntry
set blocks field $default_factory=list
set start_off 0
set entry_type $ET_FILE
set meta_json ""
# Class TOC
set entries field $default_factory=list
proc pack {self solid} {
    set out ::BytesIO $io
    ::write $out ::pack $struct "<I" len ::entries $self
    foreach e ::entries $self {
        set p ::path $e
        ::write $out ::pack $struct "<H" len $p
        ::write $out $p
        ::write $out ::pack $struct "<I" ::mode $e
        ::write $out ::pack $struct "<Q" ::mtime $e
        ::write $out ::pack $struct "<Q" ::size $e
        ::write $out ::pack $struct "<I" len ::blocks $e
        ::write $out ::pack $struct "<B" ::entry_type $e
        set meta [expr {::meta_json $e or ""}]
        ::write $out ::pack $struct "<I" len $meta
        if {$meta} {
            ::write $out $meta
        }
        if {$solid} {
            ::write $out ::pack $struct "<Q" ::start_off $e
        } else {
            foreach (idx, usz, csz, meth) ::blocks $e {
                ::write $out ::pack $struct "<Q" $idx
                ::write $out ::pack $struct "<I" $usz
                ::write $out ::pack $struct "<I" $csz
                ::write $out ::pack $struct "<B" $meth
            }
        }
    }
    ::getvalue $out
}
proc unpack {cls data solid} {
    set bio ::BytesIO $io $data
    set _tuple ::unpack $struct "<I" ::read $bio 4
    lassign $_tuple n
    set entries [list]
    foreach _ range $n {
        set _tuple ::unpack $struct "<H" ::read $bio 2
        lassign $_tuple plen
        set path ::read $bio $plen
        set _tuple ::unpack $struct "<I" ::read $bio 4
        lassign $_tuple mode
        set _tuple ::unpack $struct "<Q" ::read $bio 8
        lassign $_tuple mtime
        set _tuple ::unpack $struct "<Q" ::read $bio 8
        lassign $_tuple size
        set _tuple ::unpack $struct "<I" ::read $bio 4
        lassign $_tuple nb
        set entry_type $ET_FILE
        set meta ""
        set pos_before ::tell $bio
        # Try block
        set _tuple ::unpack $struct "<B" ::read $bio 1
        lassign $_tuple entry_type
        set _tuple ::unpack $struct "<I" ::read $bio 4
        lassign $_tuple mlen
        set meta [expr {[expr {$mlen > 0}] ? ::read $bio $mlen : ""}]
        # Catch Exception
        ::seek $bio $pos_before
        set blocks [list]
        set start_off 0
        if {$solid} {
            set _tuple ::unpack $struct "<Q" ::read $bio 8
            lassign $_tuple start_off
        } else {
            foreach _ range $nb {
                set _tuple ::unpack $struct "<Q" ::read $bio 8
                lassign $_tuple idx
                set _tuple ::unpack $struct "<I" ::read $bio 4
                lassign $_tuple usz
                set _tuple ::unpack $struct "<I" ::read $bio 4
                lassign $_tuple csz
                set _tuple ::unpack $struct "<B" ::read $bio 1
                lassign $_tuple meth
                [lappend $blocks [list $idx $usz $csz $meth]]
            }
        }
        [lappend $entries FileEntry $path $mode $mtime $size $blocks $start_off $entry_type [expr {$meta or ""}]]
    }
    cls $entries
}
proc kdf_derive_key {password header} {
    if {[expr {::kdf_id $header == $KDF_ARGON2ID}]} {
        if {[expr {not $HAVE_ARGON2}]} {
            error RuntimeError "argon2-cffi not installed"
        }
        ::hash_secret_raw $argon2ll $secret=password $salt= $time_cost= $memory_cost= $parallelism= $hash_len= $type= $version=
    }
    if {[expr {::kdf_id $header == $KDF_SCRYPT}]} {
        if {[expr {$Scrypt is ""}]} {
            error RuntimeError "cryptography not installed"
        }
        ::derive Scrypt $salt= $length= $n= $r= $p= $password
    }
    error RuntimeError "Archive not password-protected"
}
proc aead_encrypt {key header index plaintext aad} {
    if {[expr {[expr {::aead_id $header != $AEAD_AESGCM}] or [expr {not $HAVE_AESGCM}]}]} {
        error RuntimeError "AESGCM unavailable"
    }
    ::encrypt AESGCM $key nonce_from ::aead_nonce_prefix $header $index $plaintext $aad
}
proc aead_decrypt {key header index ciphertext aad} {
    if {[expr {[expr {::aead_id $header != $AEAD_AESGCM}] or [expr {not $HAVE_AESGCM}]}]} {
        error RuntimeError "AESGCM unavailable"
    }
    ::decrypt AESGCM $key nonce_from ::aead_nonce_prefix $header $index $ciphertext $aad
}
proc compress_block {method level data} {
    if {[expr {$method == $M_NONE}]} {
        $data
    }
    if {[expr {$method == $M_ZLIB}]} {
        ::compress $zlib $data [expr {[expr {[expr {1 <= $level}] <= 9}] ? $level : 6}]
    }
    if {[expr {$method == $M_LZMA}]} {
        set preset max 0 min 9 $level
        ::compress $lzma $data $format= $preset=preset
    }
    if {[expr {$method == $M_BROTLI}]} {
        if {[expr {not $HAVE_BROTLI}]} {
            error RuntimeError "brotli not installed"
        }
        ::compress $brotli $data $quality=
    }
    if {[expr {$method == $M_ZSTD}]} {
        if {[expr {not $HAVE_ZSTD}]} {
            error RuntimeError "zstandard not installed"
        }
        ::compress ::ZstdCompressor $zstd $level= $data
    }
    error RuntimeError "unknown method"
}
proc decompress_block {method data} {
    if {[expr {$method == $M_NONE}]} {
        $data
    }
    if {[expr {$method == $M_ZLIB}]} {
        ::decompress $zlib $data
    }
    if {[expr {$method == $M_LZMA}]} {
        ::decompress $lzma $data
    }
    if {[expr {$method == $M_BROTLI}]} {
        if {[expr {not $HAVE_BROTLI}]} {
            error RuntimeError "brotli not installed"
        }
        ::decompress $brotli $data
    }
    if {[expr {$method == $M_ZSTD}]} {
        if {[expr {not $HAVE_ZSTD}]} {
            error RuntimeError "zstandard not installed"
        }
        ::decompress ::ZstdDecompressor $zstd $data
    }
    error RuntimeError "unknown method"
}
proc write_footer {bw toc_offset toc_size hash_kind digest} {
    ::write $bw ::pack $struct "<Q" $toc_offset
    ::write $bw ::pack $struct "<I" $toc_size
    ::write $bw ::pack $struct "<B" $hash_kind
    if {[expr {len $digest == 32}]} {
        ::write $bw $digest
    } else {
        ::write $bw ::ljust [string range $digest 0 32] 32 "x00"
    }
    ::write $bw $END_MAGIC
}
proc read_footer {br} {
    ::seek $br [expr {- [expr {[expr {[expr {[expr {8 + 4}] + 1}] + 32}] + 5}]}] ::SEEK_END $os
    set toc_off [lindex ::unpack $struct "<Q" ::read $br 8 0]
    set toc_sz [lindex ::unpack $struct "<I" ::read $br 4 0]
    set hk [lindex ::unpack $struct "<B" ::read $br 1 0]
    set dig ::read $br 32
    if {[expr {::read $br 5 != $END_MAGIC}]} {
        error ValueError "Bad end magic"
    }
    [list $toc_off $toc_sz $hk $dig]
}
proc iter_tree {paths} {
    "Yield (pathlib.Path, lstat, entry_type). Includes dirs (even empty), symlinks, files."
    foreach p $paths {
        set pth ::Path $pathlib $p
        if {::is_dir $pth} {
            foreach root, dirs, files ::walk $os $pth {
                set rp ::Path $pathlib $root
                set st ::lstat $rp
                [list $rp $st $ET_DIR]
                foreach name $files {
                    set fp [expr {$rp / $name}]
                    set stf ::lstat $fp
                    if {::S_ISLNK $stat ::st_mode $stf} {
                        [list $fp $stf $ET_SYMLINK]
                    } else {
                        if {::S_ISREG $stat ::st_mode $stf} {
                            [list $fp $stf $ET_FILE]
                        }
                    }
                }
            }
        } else {
            set st ::lstat $pth
            if {::S_ISLNK $stat ::st_mode $st} {
                [list $pth $st $ET_SYMLINK]
            } else {
                if {::S_ISDIR $stat ::st_mode $st} {
                    [list $pth $st $ET_DIR]
                } else {
                    if {::S_ISREG $stat ::st_mode $st} {
                        [list $pth $st $ET_FILE]
                    }
                }
            }
        }
    }
}
proc hl_key {st} {
    [list getattr $st "st_dev" "" getattr $st "st_ino" ""]
}
proc posix_capture_meta {path st} {
    set meta [dict create "posix" [dict create "uid" getattr $st "st_uid" 0 "gid" getattr $st "st_gid" 0 "mode" [expr {::st_mode $st & "0o7777"}] "atime_ns" getattr $st "st_atime_ns" int [expr {::st_atime $st * 1000000000}] "mtime_ns" getattr $st "st_mtime_ns" int [expr {::st_mtime $st * 1000000000}] "ctime_ns" getattr $st "st_ctime_ns" int [expr {::st_ctime $st * 1000000000}]]]
    $meta
}
proc list_xattrs {path follow_symlinks} {
    set out [dict create]
    if {[expr {hasattr $os "listxattr" and hasattr $os "getxattr"}]} {
        # Try block
        set names ::listxattr $os $path $follow_symlinks=follow_symlinks
        foreach n $names {
            # Try block
            set v ::getxattr $os $path $n $follow_symlinks=follow_symlinks
            ::__setitem__ $out $n $v
            # Catch Exception
            # pass
        }
        # Catch Exception
        # pass
    }
    $out
}
proc apply_xattrs {path xattrs follow_symlinks} {
    if {hasattr $os "setxattr"} {
        foreach n, v [set _items [list]; foreach k [dict keys $xattrs] {lappend _items $k [dict get $xattrs $k]}; set _items] {
            # Try block
            ::setxattr $os $path $n $v $follow_symlinks=follow_symlinks
            # Catch Exception
            # pass
        }
    }
}
proc getfacl_dump {path} {
    # Try block
    set r ::run $subprocess [list "getfacl" "--absolute-names" "--tabs" "-p" "--" $path] $stdout= $stderr=
    if {[expr {::returncode $r == 0}]} {
        ::stdout $r
    }
    # Catch Exception
    # pass
    ""
}
proc setfacl_restore {text path} {
    # Try block
    set p ::Popen $subprocess [list "setfacl" "--restore=-"] $stdin= $stdout= $stderr=
    ::communicate $p $input=
    # Catch Exception
    # pass
}
proc selinux_get {path follow_symlinks} {
    # Try block
    ::getxattr $os $path "security.selinux" $follow_symlinks=follow_symlinks
    # Catch Exception
    ""
}
proc fallocate_punch_hole {fd offset length} {
    if {[expr {not $LIN}]} {
        ""
    }
    # Try block
    set libc ::CDLL $ctypes "libc.so.6" $use_errno=
    set FALLOC_FL_KEEP_SIZE 1
    set FALLOC_FL_PUNCH_HOLE 2
    set res ::fallocate $libc $fd [expr {$FALLOC_FL_PUNCH_HOLE | $FALLOC_FL_KEEP_SIZE}] ::c_longlong $ctypes $offset ::c_longlong $ctypes $length
    if {[expr {$res != 0}]} {
        # pass
    }
    # Catch Exception
    # pass
}
proc detect_sparse {path} {
    "Return list of (offset,length) holes using SEEK_HOLE/SEEK_DATA if supported; else []"
    set holes [list]
    if {[expr {not $LIN}]} {
        $holes
    }
    # Try block
    open $path "rb"
    # Catch Exception
    [list]
    $holes
}
proc win_capture_meta {path} {
    set meta [dict create]
    if {[expr {not [expr {$WIN and $HAVE_WIN32}]}]} {
        $meta
    }
    # Try block
    set attrs ::GetFileAttributesW $win32file $path
    ::__setitem__ $meta "attrs" int $attrs
    set h ::CreateFile $win32file $path ::GENERIC_READ $win32con [expr {[expr {::FILE_SHARE_READ $win32con | ::FILE_SHARE_WRITE $win32con}] | ::FILE_SHARE_DELETE $win32con}] "" ::OPEN_EXISTING $win32con ::FILE_FLAG_BACKUP_SEMANTICS $win32con ""
    # Try block
    set _tuple ::GetFileTime $win32file $h
    lassign $_tuple ct at wt
    set to_ts lambda ft {int ::timestamp $ft}
    ::__setitem__ $meta "ctime" to_ts $ct
    ::__setitem__ $meta "atime" to_ts $at
    ::__setitem__ $meta "mtime" to_ts $wt
    # Finally block
    ::Close $h
    set sd ::GetFileSecurity $win32security $path [expr {[expr {::OWNER_SECURITY_INFORMATION $win32security | ::GROUP_SECURITY_INFORMATION $win32security}] | ::DACL_SECURITY_INFORMATION $win32security}]
    ::__setitem__ $meta "sddl" ::GetSecurityDescriptorSddlForm $sd [expr {[expr {::OWNER_SECURITY_INFORMATION $win32security | ::GROUP_SECURITY_INFORMATION $win32security}] | ::DACL_SECURITY_INFORMATION $win32security}]
    set ads [list]
    # Try block
    foreach s ::FindStreamsW $win32file $path {
        set name [lindex $s 0]
        if {[expr {$name in [list ":\$DATA" "::\$DATA"]}]} {
            # pass
        }
        # Try block
        open [expr {$path + $name}] "rb"
        # Catch Exception
        [lappend $ads [dict create "name" $name "hex" ""]]
    }
    # Catch Exception
    # pass
    if {$ads} {
        ::__setitem__ $meta "ads" $ads
    }
    # Catch Exception
    # pass
    $meta
}
proc win_apply_meta {path meta is_dir} {
    if {[expr {not [expr {$WIN and $HAVE_WIN32}]}]} {
        ""
    }
    # Try block
    if {[expr {"attrs" in $meta}]} {
        ::SetFileAttributesW $win32file $path int [lindex $meta "attrs"]
    }
    # Catch Exception
    # pass
    if {any [expr {$k in $meta}]} {
        # Try block
        set h ::CreateFile $win32file $path ::GENERIC_WRITE $win32con [expr {[expr {::FILE_SHARE_READ $win32con | ::FILE_SHARE_WRITE $win32con}] | ::FILE_SHARE_DELETE $win32con}] "" ::OPEN_EXISTING $win32con ::FILE_FLAG_BACKUP_SEMANTICS $win32con ""
proc to_ft {ts} {
            ::Time $pywintypes float $ts
}
        set ct [expr {[expr {"ctime" in $meta}] ? to_ft ::get $meta "ctime" : ""}]
        set at [expr {[expr {"atime" in $meta}] ? to_ft ::get $meta "atime" : ""}]
        set mt [expr {[expr {"mtime" in $meta}] ? to_ft ::get $meta "mtime" : ""}]
        ::SetFileTime $win32file $h $ct $at $mt
        ::Close $h
        # Catch Exception
        # pass
    }
    if {[expr {"sddl" in $meta}]} {
        # Try block
        set sd ::ConvertStringSecurityDescriptorToSecurityDescriptor $win32security [lindex $meta "sddl"] ::SDDL_REVISION_1 $win32security
        ::SetFileSecurity $win32security $path [expr {[expr {::DACL_SECURITY_INFORMATION $win32security | ::OWNER_SECURITY_INFORMATION $win32security}] | ::GROUP_SECURITY_INFORMATION $win32security}] $sd
        # Catch Exception
        # pass
    }
    if {[expr {"ads" in $meta}]} {
        foreach s [lindex $meta "ads"] {
            # Try block
            if {[expr {::get $s "hex" is not ""}]} {
                set data ::fromhex $bytes [lindex $s "hex"]
                open [expr {$path + [lindex $s "name"]}] "wb"
            }
            # Catch Exception
            # pass
        }
    }
}
proc _load_header_toc_and_key {br need_password} {
    set header ::unpack $HeaderV1 $br
    set _tuple read_footer $br
    lassign $_tuple toc_off toc_sz hk dig
    ::seek $br $toc_off
    set toc_data ::read $br $toc_sz
    set key ""
    if {[expr {::flags $header & $F_ENCRYPTED}]} {
        if {[expr {not $need_password}]} {
            error SystemExit "Archive is encrypted; use --password"
        }
        set pw ::getpass $getpass "Password: "
        set key kdf_derive_key $pw $header
        set toc_data aead_decrypt $key $header -1 $toc_data $aad=
    }
    set toc ::unpack $TOC $toc_data $solid=
    [list $header $toc $key $toc_off $toc_sz $hk $dig]
}
proc _recompute_hash_until {bf upto hash_kind} {
    ::seek $bf 0
    set h make_hasher $hash_kind
    set done 0
    while {[expr {$done < $upto}]} {
        set chunk ::read $bf min [expr {1024 * 1024}] [expr {$upto - $done}]
        if {[expr {not $chunk}]} {
            # pass
        }
        hasher_update $h $chunk $hash_kind
        set done [expr {$done + len $chunk}]
    }
    hasher_digest $h $hash_kind
}
proc cmd_create {args} {
    set block_size [expr {1 << ::block_exp $args}]
    set method ::get $NAME_TO_METHOD ::method $args
    if {[expr {$method is ""}]} {
        error SystemExit "Unknown method {args.method}"
    }
    set header HeaderV1
    setattr $header "default_method" $method
    setattr $header "default_level" ::level $args
    setattr $header "block_exp" ::block_exp $args
    setattr $header "threads_hint" ::threads $args
    setattr $header "ram_mib_hint" ::max_ram_mib $args
    if {::solid $args} {
        setattr $header "flags" [expr {getattr $header "flags" + $F_SOLID}]
    }
    set key ""
    if {::password $args} {
        if {[expr {not $HAVE_AESGCM}]} {
            error SystemExit "cryptography not installed; cannot encrypt"
        }
        if {$HAVE_ARGON2} {
            setattr $header "kdf_id" $KDF_ARGON2ID
            setattr $header "kdf_t" [expr {::kdf_time $args or 3}]
            setattr $header "kdf_m" [expr {::kdf_mem_kib $args or [expr {256 * 1024}]}]
            setattr $header "kdf_p" [expr {::kdf_parallel $args or 4}]
        } else {
            setattr $header "kdf_id" $KDF_SCRYPT
            setattr $header "kdf_t" [expr {::scrypt_n $args or [expr {1 << 15}]}]
            setattr $header "kdf_m" [expr {::scrypt_r $args or 8}]
            setattr $header "kdf_p" [expr {::scrypt_p $args or 1}]
        }
        setattr $header "salt" ::urandom $os 16
        setattr $header "aead_id" $AEAD_AESGCM
        setattr $header "aead_nonce_prefix" ::urandom $os 12
        setattr $header "flags" [expr {getattr $header "flags" + $F_ENCRYPTED}]
        ::info $LOGGER "Encryption enabled (AES-256-GCM)."
        set pw ::getpass $getpass "Password: "
        set key kdf_derive_key $pw $header
    }
}
set toc TOC
set block_index 0
set hardlinks [dict create]
open ::output $args "wb"
set items list iter_tree ::inputs $args
if {[expr {[expr {::flags $header & $F_SOLID}] and [expr {::solid_by $args == "ext"}]}]} {
proc ext_key {item} {
        set _tuple $item
        lassign $_tuple fp st et
        set e [string tolower ::suffix $fp]
        [list [expr {$e ? $e : ""}] str $fp]
}
    ::sort $items $key=ext_key
}
set file_items [list [foreach it $items {if {[expr {[lindex $it 2] == $ET_FILE}]} {lappend _result $it}}]
set total_files len $file_items
set total_bytes sum int getattr $st "st_size" 0 $st [expr {$_ in $file_items}]
set prog Progress $total_files $total_bytes
::info $LOGGER "Preparing to compress {total_files} files ({human_bytes(total_bytes)}). Solid={bool(args.solid)} method={args.method} lvl={args.level}"
if {[expr {[expr {::flags $header & $F_SOLID}] and [expr {::solid_chunk_exp $args is not ""}]}]} {
    ::info $LOGGER "Solid chunk size ≈ {human_bytes(1<<int(args.solid_chunk_exp))}"
}
set solid_buffer ::BytesIO $io
set cur_off 0
foreach fp, st, et $items {
    set rel str $fp
    set st_mode [expr {::st_mode $st & "0o7777"}]
    set st_mtime int getattr $st "st_mtime" ::time $time
    set meta_obj [dict create]
}
if {[expr {$LIN and ::posixmeta $args}]} {
    ::update $meta_obj posix_capture_meta $rel $st
}
if {[expr {$WIN and ::winmeta $args}]} {
    ::__setitem__ $meta_obj "win" win_capture_meta $rel
}
if {[expr {$LIN and ::xattrs $args}]} {
    set x list_xattrs $rel $follow_symlinks=
    if {$x} {
        ::__setitem__ $meta_obj "xattrs" [dict create {*}[set _result [dict create]; foreach {k v} [set _items [list]; foreach k [dict keys $x] {lappend _items $k [dict get $x $k]}; set _items] {dict set _result $k ::hex $v}; set _result]]
    }
}
if {[expr {$LIN and ::selinux $args}]} {
    set sctx selinux_get $rel $follow_symlinks=
    if {[expr {$sctx is not ""}]} {
        ::__setitem__ ::setdefault $meta_obj "xattrs" [dict create] "security.selinux" ::hex $sctx
        ::__setitem__ $meta_obj "selinux" $sctx
    }
}
if {[expr {$et == $ET_DIR}]} {
    set entry FileEntry $rel $st_mode $st_mtime 0 [list] 0 $ET_DIR [expr {$meta_obj ? ::dumps $json $meta_obj $ensure_ascii= : ""}]
    [lappend ::entries $toc $entry]
    if {[expr {::level $LOGGER >= [lindex ::LEVELS $VLog "trace"]}]} {
        ::trace $LOGGER "Discovered directory {rel}"
    }
    # pass
}
if {[expr {[expr {$et == $ET_SYMLINK}] and $LIN}]} {
    set target ::readlink $os $rel
    ::__setitem__ $meta_obj "link_target" $target
    set entry FileEntry $rel $st_mode $st_mtime 0 [list] 0 $ET_SYMLINK ::dumps $json $meta_obj $ensure_ascii=
    [lappend ::entries $toc $entry]
    if {[expr {::level $LOGGER >= [lindex ::LEVELS $VLog "trace"]}]} {
        ::trace $LOGGER "Recorded symlink {rel} -> {target}"
    }
    # pass
}
if {[expr {[expr {$LIN and [expr {$et == $ET_FILE}]}] and [expr {getattr $st "st_nlink" 1 > 1}]}]} {
    set key_hl hl_key $st
    if {[expr {$key_hl in $hardlinks}]} {
        ::__setitem__ $meta_obj "hardlink_to" [lindex $hardlinks $key_hl]
        set entry FileEntry $rel $st_mode $st_mtime 0 [list] 0 $ET_HARDLINK ::dumps $json $meta_obj $ensure_ascii=
        [lappend ::entries $toc $entry]
        if {[expr {::level $LOGGER >= [lindex ::LEVELS $VLog "trace"]}]} {
            ::trace $LOGGER "Recorded hardlink {rel} -> {hardlinks[key_hl]}"
        }
        # pass
    } else {
        ::__setitem__ $hardlinks $key_hl $rel
    }
}
set size int ::st_size $st
if {[expr {[expr {$LIN and ::sparse $args}] and ::S_ISREG $stat ::st_mode $st}]} {
    set holes detect_sparse $rel
    if {$holes} {
        ::__setitem__ $meta_obj "holes" $holes
    }
}
set meta_bytes [expr {$meta_obj ? ::dumps $json $meta_obj $ensure_ascii= : ""}]
if {[expr {::flags $header & $F_SOLID}]} {
    if {[expr {::level $LOGGER >= [lindex ::LEVELS $VLog "debug"]}]} {
        ::debug $LOGGER "Queuing file {rel} ({human_bytes(size)}) for solid stream"
    }
    set t0 ::time $time
    open $rel "rb"
    set duration [expr {::time $time - $t0}]
    ::add_file $prog $size $duration
    set _tuple ::estimate $prog
    lassign $_tuple elapsed eta rate ratio
    if {[expr {::level $LOGGER >= [lindex ::LEVELS $VLog "debug"]}]} {
        set arch_so_far ::tell $bw
        ::debug $LOGGER "Queued {rel} in {duration:.2f}s | {prog.done_files}/{prog.total_files} files, {human_bytes(prog.done_bytes)}/{human_bytes(prog.total_bytes)} | arch {human_bytes(arch_so_far)} | elapsed {elapsed:.1f}s | eta {('∞' if eta==float('inf') else f'{eta:.1f}s')}"
    }
    set entry FileEntry $rel $st_mode $st_mtime $size [list] $cur_off $ET_FILE $meta_bytes
    set cur_off [expr {$cur_off + $size}]
    [lappend ::entries $toc $entry]
} else {
    if {[expr {::level $LOGGER >= [lindex ::LEVELS $VLog "debug"]}]} {
        ::debug $LOGGER "Compressing file {rel} ({human_bytes(size)})"
    }
    set t0 ::time $time
    set entry FileEntry $rel $st_mode $st_mtime $size [list] 0 $ET_FILE $meta_bytes
    open $rel "rb"
    set duration [expr {::time $time - $t0}]
    [lappend ::entries $toc $entry]
    ::add_file $prog $size $duration
    set arch_so_far ::tell $bw
    set _tuple ::estimate $prog
    lassign $_tuple elapsed eta rate ratio
    if {[expr {::level $LOGGER >= [lindex ::LEVELS $VLog "debug"]}]} {
        set saved max 0 [expr {::done_bytes $prog - $arch_so_far}]
        set ratio_now [expr {::done_bytes $prog ? [expr {$arch_so_far / ::done_bytes $prog}] : 0}]
        ::debug $LOGGER "Done {rel} in {duration:.2f}s | {prog.done_files}/{prog.total_files} files, {human_bytes(prog.done_bytes)}/{human_bytes(prog.total_bytes)} | arch {human_bytes(arch_so_far)} | saved {human_bytes(saved)} | ratio {ratio_now:.3f} | elapsed {elapsed:.1f}s | eta {('∞' if eta==float('inf') else f'{eta:.1f}s')}"
    }
}
if {[expr {::flags $header & $F_SOLID}]} {
    set whole ::getvalue $solid_buffer
    if {[expr {::solid_chunk_exp $args is not ""}]} {
        set seg_size [expr {1 << int ::solid_chunk_exp $args}]
        if {[expr {::level $LOGGER >= [lindex ::LEVELS $VLog "trace"]}]} {
            ::trace $LOGGER "Emitting solid chunks of ~{human_bytes(seg_size)}"
        }
        set pos 0
        set total len $whole
        while {[expr {$pos < $total}]} {
            set seg [string range $whole $pos [expr {$pos + $seg_size}]]
            set pos [expr {$pos + len $seg}]
            set comp compress_block $method ::level $args $seg
            set payload [expr {[expr {$key is ""}] ? $comp : aead_encrypt $key $header $block_index $comp $aad=}]
            ::write $bw ::pack $struct "<I" len $payload
            ::write $bw ::pack $struct "<B" $method
            ::write $bw $payload
            hasher_update $hasher [expr {[expr {::pack $struct "<I" len $payload + ::pack $struct "<B" $method}] + $payload}] default_hash_kind
            set block_index [expr {$block_index + 1}]
        }
    } else {
        set comp compress_block $method ::level $args $whole
        set payload [expr {[expr {$key is ""}] ? $comp : aead_encrypt $key $header $block_index $comp $aad=}]
        ::write $bw ::pack $struct "<I" len $payload
        ::write $bw ::pack $struct "<B" $method
        ::write $bw $payload
        hasher_update $hasher [expr {[expr {::pack $struct "<I" len $payload + ::pack $struct "<B" $method}] + $payload}] default_hash_kind
        set block_index [expr {$block_index + 1}]
    }
    set arch_so_far ::tell $bw
    set saved max 0 [expr {::done_bytes $prog - $arch_so_far}]
    set ratio_now [expr {::done_bytes $prog ? [expr {$arch_so_far / ::done_bytes $prog}] : 0}]
    ::info $LOGGER "Solid stream written | arch {human_bytes(arch_so_far)} | src {human_bytes(prog.done_bytes)} | saved {human_bytes(saved)} | ratio {ratio_now:.3f}"
}
set toc_data ::pack $toc $solid=
::trace $LOGGER "Writing TOC..."
if {[expr {$key is not ""}]} {
    set toc_data aead_encrypt $key $header -1 $toc_data $aad=
}
set toc_off ::tell $bw
::write $bw $toc_data
set toc_sz len $toc_data
hasher_update $hasher $toc_data default_hash_kind
set digest hasher_digest $hasher default_hash_kind
write_footer $bw $toc_off $toc_sz default_hash_kind $digest
set arch_final ::tell $bw
set _tuple ::estimate $prog
lassign $_tuple elapsed eta rate ratio
set saved max 0 [expr {::done_bytes $prog - $arch_final}]
set ratio_final [expr {::done_bytes $prog ? [expr {$arch_final / ::done_bytes $prog}] : 0}]
::info $LOGGER "Done in {elapsed:.2f}s | files {prog.done_files}/{prog.total_files} | src {human_bytes(prog.done_bytes)} | arch {human_bytes(arch_final)} | saved {human_bytes(saved)} | ratio {ratio_final:.3f}"
puts "Created {args.output} with {len(toc.entries)} entry(s). Solid={bool(header.flags & F_SOLID)}"
proc cmd_list {args} {
    open ::archive $args "rb"
}
proc cmd_test {args} {
    ::info $LOGGER "Verifying archive footer hash and block integrity..."
    open ::archive $args "rb"
}
proc cmd_extract {args} {
    set outdir ::Path $pathlib [expr {::output $args or "."}]
    ::mkdir $outdir $parents= $exist_ok=
    open ::archive $args "rb"
}
set dirs [list [foreach e ::entries $toc {if {[expr {::entry_type $e == $ET_DIR}]} {lappend _result $e}}]
set syms [list [foreach e ::entries $toc {if {[expr {::entry_type $e == $ET_SYMLINK}]} {lappend _result $e}}]
set hlinks [list [foreach e ::entries $toc {if {[expr {::entry_type $e == $ET_HARDLINK}]} {lappend _result $e}}]
set files [list [foreach e ::entries $toc {if {[expr {::entry_type $e == $ET_FILE}]} {lappend _result $e}}]
foreach e $dirs {
    set out_path [expr {$outdir / ::path $e}]
    ::mkdir $out_path $parents= $exist_ok=
    # Try block
    ::chmod $os $out_path ::mode $e
    # Catch Exception
    # pass
    # Try block
    ::utime $os $out_path [list ::mtime $e ::mtime $e]
    # Catch Exception
    # pass
    if {[expr {::meta_json $e and $WIN}]} {
        # Try block
        win_apply_meta str $out_path ::loads $json ::meta_json $e 1
        # Catch Exception
        # pass
    }
    if {[expr {[expr {::meta_json $e and $LIN}] and ::posixmeta $args}]} {
        # Try block
        set meta ::loads $json ::meta_json $e
        set pos ::get $meta "posix" [dict create]
        # Try block
        ::chown $os $out_path ::get $pos "uid" [expr {- 1}] ::get $pos "gid" [expr {- 1}]
        # Catch Exception
        # pass
        if {[expr {::xattrs $args and [expr {"xattrs" in $meta}]}]} {
            apply_xattrs str $out_path [dict create {*}[set _result [dict create]; foreach {k v} [set _items [list]; foreach k [dict keys [lindex $meta "xattrs"]] {lappend _items $k [dict get [lindex $meta "xattrs"] $k]}; set _items] {dict set _result $k ::fromhex $bytes $v}; set _result]] $follow_symlinks=
        }
        if {[expr {::acl $args and ::get $meta "acl"}]} {
            setfacl_restore [lindex $meta "acl"] str $out_path
        }
        # Catch Exception
        # pass
    }
    ::trace $LOGGER "Created directory {e.path}"
}
set solid_concat ""
if {[expr {::flags $header & $F_SOLID}]} {
    set parts [list]
    while {[expr {::tell $br < $toc_off}]} {
        set hdr ::read $br 5
        if {[expr {len $hdr < 5}]} {
            # pass
        }
        set blen [lindex ::unpack $struct "<I" [string range $hdr 0 4] 0]
        set meth [lindex $hdr 4]
        set payload ::read $br $blen
        if {[expr {$key is not ""}]} {
            set payload aead_decrypt $key $header 0 $payload $aad=
        }
        [lappend $parts decompress_block ::default_method $header $payload]
    }
    set solid_concat [join $parts ""]
}
foreach e $syms {
    set out_path [expr {$outdir / ::path $e}]
    ::mkdir ::parent $out_path $parents= $exist_ok=
    set target ""
    # Try block
    set meta ::loads $json ::meta_json $e
    set target ::get $meta "link_target" ""
    # Catch Exception
    # pass
    # Try block
    if {$LIN} {
        # Try block
        ::remove $os $out_path
        # Catch Exception
        # pass
        ::symlink $os $target $out_path
        # Try block
        ::lchmod $os $out_path ::mode $e
        # Catch Exception
        # pass
        if {[expr {::posixmeta $args and [expr {"posix" in $meta}]}]} {
            set pos [lindex $meta "posix"]
            # Try block
            ::lchown $os $out_path ::get $pos "uid" [expr {- 1}] ::get $pos "gid" [expr {- 1}]
            # Catch Exception
            # pass
        }
        if {[expr {::xattrs $args and [expr {"xattrs" in $meta}]}]} {
            apply_xattrs str $out_path [dict create {*}[set _result [dict create]; foreach {k v} [set _items [list]; foreach k [dict keys [lindex $meta "xattrs"]] {lappend _items $k [dict get [lindex $meta "xattrs"] $k]}; set _items] {dict set _result $k ::fromhex $bytes $v}; set _result]] $follow_symlinks=
        }
    }
    # Catch Exception
    # pass
    ::trace $LOGGER "Created symlink {e.path} -> {target}"
}
foreach e $files {
    set out_path [expr {$outdir / ::path $e}]
    ::mkdir ::parent $out_path $parents= $exist_ok=
    if {[expr {::flags $header & $F_SOLID}]} {
        set segment [string range $solid_concat ::start_off $e [expr {::start_off $e + ::size $e}]]
        open $out_path "wb"
    } else {
        open $out_path "wb"
    }
    # Try block
    ::chmod $os $out_path ::mode $e
    # Catch Exception
    # pass
    # Try block
    ::utime $os $out_path [list ::mtime $e ::mtime $e]
    # Catch Exception
    # pass
}
if {[expr {::meta_json $e and $WIN}]} {
    # Try block
    win_apply_meta str $out_path ::loads $json ::meta_json $e 0
    # Catch Exception
    # pass
}
if {[expr {[expr {::meta_json $e and $LIN}] and ::posixmeta $args}]} {
    # Try block
    set meta ::loads $json ::meta_json $e
    set pos ::get $meta "posix" [dict create]
    # Try block
    ::chown $os $out_path ::get $pos "uid" [expr {- 1}] ::get $pos "gid" [expr {- 1}]
    # Catch Exception
    # pass
    if {[expr {::xattrs $args and [expr {"xattrs" in $meta}]}]} {
        apply_xattrs str $out_path [dict create {*}[set _result [dict create]; foreach {k v} [set _items [list]; foreach k [dict keys [lindex $meta "xattrs"]] {lappend _items $k [dict get [lindex $meta "xattrs"] $k]}; set _items] {dict set _result $k ::fromhex $bytes $v}; set _result]] $follow_symlinks=
    }
    if {[expr {::acl $args and ::get $meta "acl"}]} {
        setfacl_restore [lindex $meta "acl"] str $out_path
    }
    if {[expr {[expr {::sparse $args and [expr {"holes" in $meta}]}] and $LIN}]} {
        open $out_path "r+b"
    }
    # Catch Exception
    # pass
}
::debug $LOGGER "Extracted {e.path}"
foreach e $hlinks {
    set out_path [expr {$outdir / ::path $e}]
    ::mkdir ::parent $out_path $parents= $exist_ok=
    set target ""
    # Try block
    set meta ::loads $json ::meta_json $e
    set target ::get $meta "hardlink_to" ""
    # Catch Exception
    # pass
    # Try block
    if {$target} {
        set src [expr {$outdir / $target}]
        if {::exists $src} {
            # Try block
            if {::exists $out_path} {
                ::remove $os $out_path
            }
            # Catch Exception
            # pass
            ::link $os $src $out_path
            ::trace $LOGGER "Created hardlink {e.path} -> {target}"
        }
    }
    # Catch Exception
    # pass
}
proc cmd_append {args} {
    open ::archive $args "r+b"
}
set items list iter_tree ::inputs $args
set file_items [list [foreach it $items {if {[expr {[lindex $it 2] == $ET_FILE}]} {lappend _result $it}}]
set total_files len $file_items
set total_bytes sum int getattr $st "st_size" 0 $st [expr {$_ in $file_items}]
set prog Progress $total_files $total_bytes
::info $LOGGER "Appending {total_files} files ({human_bytes(total_bytes)})..."
foreach fp, st, et $items {
    if {[expr {$et != $ET_FILE}]} {
        ::trace $LOGGER "Skipping non-file during append: {fp}"
        # pass
    }
    set rel str $fp
    set st_mode [expr {::st_mode $st & "0o7777"}]
    set st_mtime int getattr $st "st_mtime" ::time $time
    set size int ::st_size $st
    set entry FileEntry $rel $st_mode $st_mtime $size [list] 0 $ET_FILE
    if {[expr {::level $LOGGER >= [lindex ::LEVELS $VLog "debug"]}]} {
        ::debug $LOGGER "Compressing file {rel} ({human_bytes(size)})"
    }
    set t0 ::time $time
    open $rel "rb"
    [lappend ::entries $toc $entry]
    set duration [expr {::time $time - $t0}]
    ::add_file $prog $size $duration
    set arch_so_far ::tell $f
    set _tuple ::estimate $prog
    lassign $_tuple elapsed eta rate ratio
    if {[expr {::level $LOGGER >= [lindex ::LEVELS $VLog "debug"]}]} {
        set saved max 0 [expr {::done_bytes $prog - $arch_so_far}]
        set ratio_now [expr {::done_bytes $prog ? [expr {$arch_so_far / ::done_bytes $prog}] : 0}]
        ::debug $LOGGER "Done {rel} in {duration:.2f}s | {prog.done_files}/{prog.total_files} files, {human_bytes(prog.done_bytes)}/{human_bytes(prog.total_bytes)} | arch {human_bytes(arch_so_far)} | saved {human_bytes(saved)} | ratio {ratio_now:.3f} | elapsed {elapsed:.1f}s | eta {('∞' if eta==float('inf') else f'{eta:.1f}s')}"
    }
}
set toc_data ::pack $toc $solid=
::trace $LOGGER "Writing updated TOC (append)..."
if {[expr {$key is not ""}]} {
    set toc_data aead_encrypt $key $header -1 $toc_data $aad=
}
set toc_new_off ::tell $f
::write $f $toc_data
set toc_new_sz len $toc_data
set upto ::tell $f
set digest _recompute_hash_until $f $upto [expr {[expr {$hash_kind != $H_NONE}] ? $hash_kind : default_hash_kind}]
write_footer $f $toc_new_off $toc_new_sz [expr {[expr {$hash_kind != $H_NONE}] ? $hash_kind : default_hash_kind}] $digest
set arch_final ::tell $f
set _tuple ::estimate $prog
lassign $_tuple elapsed eta rate ratio
set saved max 0 [expr {::done_bytes $prog - [expr {$arch_final - $toc_new_sz}]}]
::info $LOGGER "Append done in {elapsed:.2f}s | files {prog.done_files}/{prog.total_files} | added {human_bytes(prog.done_bytes)} | archive now {human_bytes(arch_final)}"
proc build_argparser {} {
    set ap ::ArgumentParser $argparse $prog= $description=
    set sub ::add_subparsers $ap $dest= $required=
}
set ap_c ::add_parser $sub "c" $help=
::add_argument $ap_c "output" $help=
::add_argument $ap_c "inputs" $nargs= $help=
::add_argument $ap_c "--method" $default= $choices= $help=
::add_argument $ap_c "--level" $type=int $default= $help=
::add_argument $ap_c "--block-exp" $type=int $default= $dest= $help=
::add_argument $ap_c "--threads" $type=int $default= $help=
::add_argument $ap_c "--max-ram-mib" $type=int $default= $help=
::add_argument $ap_c "--password" $action= $help=
::add_argument $ap_c "--solid" $action= $help=
::add_argument $ap_c "--solid-chunk-exp" $type=int $default= $help=
::add_argument $ap_c "--solid-by" $choices= $default= $help=
::add_argument $ap_c "--winmeta" $action= $help=
::add_argument $ap_c "--posixmeta" $action= $help=
::add_argument $ap_c "--xattrs" $action= $help=
::add_argument $ap_c "--acl" $action= $help=
::add_argument $ap_c "--selinux" $action= $help=
::add_argument $ap_c "--sparse" $action= $help=
::add_argument $ap_c "--kdf-time" $type=int $default= $help=
::add_argument $ap_c "--kdf-mem-kib" $type=int $default= $help=
::add_argument $ap_c "--kdf-parallel" $type=int $default= $help=
::add_argument $ap_c "--scrypt-n" $type=int $default= $help=
::add_argument $ap_c "--scrypt-r" $type=int $default= $help=
::add_argument $ap_c "--scrypt-p" $type=int $default= $help=
set ap_a ::add_parser $sub "a" $help=
::add_argument $ap_a "archive"
::add_argument $ap_a "inputs" $nargs=
::add_argument $ap_a "--method" $default= $choices= $help=
::add_argument $ap_a "--level" $type=int $default= $help=
::add_argument $ap_a "--password" $action= $help=
set ap_l ::add_parser $sub "l" $help=
::add_argument $ap_l "archive"
::add_argument $ap_l "--password" $action= $help=
set ap_t ::add_parser $sub "t" $help=
::add_argument $ap_t "archive"
::add_argument $ap_t "--password" $action= $help=
set ap_x ::add_parser $sub "x" $help=
::add_argument $ap_x "archive"
::add_argument $ap_x "-o" "--output" $default= $help=
::add_argument $ap_x "--password" $action= $help=
foreach sp [list $ap_c $ap_a $ap_l $ap_t $ap_x] {
    ::add_argument $sp "--log-level" $choices= $default= $help=
    ::add_argument $sp "-v" "--verbose" $action= $help=
}
$ap
proc main {argv} {
    set ap build_argparser
    set args ::parse_args $ap $argv
    if {[expr {getattr $args "verbose" 0 and [expr {getattr $args "log_level" "" == "warning"}]}]} {
        setattr $args "log_level" "info"
    }
    $global
    $LOGGER
    set LOGGER VLog getattr $args "log_level" "warning"
}
if {[expr {::cmd $args == "c"}]} {
    cmd_create $args
} else {
    if {[expr {::cmd $args == "a"}]} {
        cmd_append $args
    } else {
        if {[expr {::cmd $args == "l"}]} {
            cmd_list $args
        } else {
            if {[expr {::cmd $args == "t"}]} {
                cmd_test $args
            } else {
                if {[expr {::cmd $args == "x"}]} {
                    cmd_extract $args
                } else {
                    ::print_help $ap
                }
            }
        }
    }
}
if {[expr {$__name__ == "__main__"}]} {
    main
}
