#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
UO 客户端加密密钥计算器
根据客户端版本号计算加密密钥 (key1, key2, key3)
"""

def calculate_encryption_keys(major, minor, build, revision=0):
    """
    计算 UO 加密密钥
    
    参数:
        major: 主版本号
        minor: 次版本号
        build: 构建号
        revision: 修订号 (默认 0)
    
    返回:
        (key1, key2, key3, encryption_type)
    """
    a = major
    b = minor
    c = build
    
    # 计算 key2
    temp = ((((a << 9) | b) << 10) | c) ^ ((c * c) << 5)
    
    key2 = ((temp << 4) ^ (b * b) ^ (b * 0x0B000000) ^ (c * 0x380000) ^ 0x2C13A5FD) & 0xFFFFFFFF
    
    # 计算 key3
    temp = (((((a << 9) | c) << 10) | b) * 8) ^ (c * c * 0x0c00)
    
    key3 = (temp ^ (b * b) ^ (b * 0x6800000) ^ (c * 0x1c0000) ^ 0x0A31D527F) & 0xFFFFFFFF
    
    # 计算 key1
    key1 = (key2 - 1) & 0xFFFFFFFF
    
    # 确定加密类型
    version_int = (major << 24) | (minor << 16) | (build << 8) | revision
    
    if version_int == 0x02000000:  # 2.0.0.0
        enc_type = "BLOWFISH__2_0_3"
    elif version_int < 0x01190000:  # < 1.25.0.0
        enc_type = "OLD_BFISH"
    elif version_int == 0x01192400:  # 1.25.36.0
        enc_type = "BLOWFISH__1_25_36"
    elif version_int <= 0x02000000:  # <= 2.0.0.0
        enc_type = "BLOWFISH"
    elif version_int <= 0x02000300:  # <= 2.0.3.0
        enc_type = "BLOWFISH__2_0_3"
    else:
        enc_type = "TWOFISH_MD5"
    
    return key1, key2, key3, enc_type


def format_version(major, minor, build, revision=0):
    """格式化版本号显示"""
    return f"{major}.{minor}.{build}.{revision:02d}"


def print_keys(major, minor, build, revision=0):
    """打印加密密钥信息"""
    key1, key2, key3, enc_type = calculate_encryption_keys(major, minor, build, revision)
    
    version_str = format_version(major, minor, build, revision)
    version_hex = f"{major:02X}{minor:02X}{build:02X}{revision:02X}"
    
    print(f"\n{'='*60}")
    print(f"版本: {version_str}")
    print(f"版本 (Hex): {version_hex}")
    print(f"{'='*60}")
    print(f"Key1: 0x{key1:08X} ({key1})")
    print(f"Key2: 0x{key2:08X} ({key2})")
    print(f"Key3: 0x{key3:08X} ({key3})")
    print(f"加密类型: {enc_type}")
    print(f"{'='*60}\n")
    
    return key1, key2, key3, enc_type


def main():
    """主函数"""
    print("UO 客户端加密密钥计算器")
    print("="*60)
    
    # 测试几个版本
    test_versions = [
        (7, 0, 111, 0),   # 7.0.111.0
        (7, 0, 110, 0),   # 7.0.110.0
        (7, 0, 109, 0),   # 7.0.109.0
        (7, 0, 100, 0),   # 7.0.100.0
        (6, 0, 14, 2),    # 6.0.14.2
        (5, 0, 9, 1),     # 5.0.9.1
        (4, 0, 11, 2),    # 4.0.11.2
        (2, 0, 3, 0),     # 2.0.3.0
        (2, 0, 0, 0),     # 2.0.0.0
        (1, 25, 36, 0),   # 1.25.36.0
        (1, 25, 35, 0),   # 1.25.35.0
    ]
    
    for major, minor, build, revision in test_versions:
        print_keys(major, minor, build, revision)
    
    # 交互模式
    print("\n" + "="*60)
    print("交互模式 (输入 'q' 退出)")
    print("="*60)
    
    while True:
        try:
            user_input = input("\n请输入版本号 (格式: major.minor.build 或 major.minor.build.revision): ").strip()
            
            if user_input.lower() == 'q':
                print("退出程序")
                break
            
            parts = user_input.split('.')
            
            if len(parts) < 3:
                print("错误: 版本号格式不正确，至少需要 major.minor.build")
                continue
            
            major = int(parts[0])
            minor = int(parts[1])
            build = int(parts[2])
            revision = int(parts[3]) if len(parts) > 3 else 0
            
            print_keys(major, minor, build, revision)
            
        except ValueError:
            print("错误: 请输入有效的数字")
        except KeyboardInterrupt:
            print("\n\n退出程序")
            break
        except Exception as e:
            print(f"错误: {e}")


if __name__ == "__main__":
    main()
